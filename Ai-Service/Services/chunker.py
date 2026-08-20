import re

from Models.schemas import ChunkItem
from Services.embedder import count_tokens, max_sequence_length


def chunk_text(text: str, max_tokens_per_chunk: int = 300) -> list[ChunkItem]:
    """
    Structure-aware chunking, not fixed-size sliding windows -- see the
    Phase 2 design notes for why. Strategy:
      1. Split on paragraph breaks (blank lines) first -- these already exist
         in the extracted text from Phase 1's PdfTextExtractor/DocxTextExtractor.
      2. Track page numbers via the "\f" page-break markers PdfTextExtractor
         inserts between pages.
      3. Accumulate paragraphs into a chunk until adding the next paragraph
         would exceed max_tokens_per_chunk, then start a new chunk.
      4. A single paragraph that alone exceeds the token ceiling is split
         further on sentence boundaries as a fallback -- this should be rare
         for well-formed documents.

    """
    max_tokens_per_chunk = min(max_tokens_per_chunk, max_sequence_length())

    chunks: list[ChunkItem] = []
    current_page = 1
    current_parts: list[str] = []
    current_tokens = 0
    chunk_index = 0

    # Split on double-newlines (paragraph breaks) while keeping page-break
    # markers as their own tokens so we can track page numbers as we go.
    raw_blocks = re.split(r"(\n\s*\n|\f)", text)

    def flush():
        nonlocal current_parts, current_tokens, chunk_index
        if not current_parts:
            return
        chunk_text_value = "\n\n".join(p.strip() for p in current_parts if p.strip())
        if chunk_text_value:
            chunks.append(ChunkItem(
                chunk_index=chunk_index,
                text=chunk_text_value,
                page_number=current_page,
                section_heading=None,  # heading detection is a documented future improvement, not attempted here
            ))
            chunk_index += 1
        current_parts = []
        current_tokens = 0

    for block in raw_blocks:
        if block == "\f":
            flush()
            current_page += 1
            continue
        if not block or block.strip() == "" or re.fullmatch(r"\n\s*\n", block):
            continue

        paragraph = block.strip()
        if not paragraph:
            continue

        paragraph_tokens = count_tokens(paragraph)

        if paragraph_tokens > max_tokens_per_chunk:
            # Oversized single paragraph -- flush what we have, then split
            # this paragraph on sentence boundaries as a fallback.
            flush()
            for piece in _split_oversized_paragraph(paragraph, max_tokens_per_chunk):
                chunks.append(ChunkItem(
                    chunk_index=chunk_index,
                    text=piece,
                    page_number=current_page,
                    section_heading=None,
                ))
                chunk_index += 1
            continue

        if current_tokens + paragraph_tokens > max_tokens_per_chunk and current_parts:
            flush()

        current_parts.append(paragraph)
        current_tokens += paragraph_tokens

    flush()
    return chunks


def _split_oversized_paragraph(paragraph: str, max_tokens: int) -> list[str]:
    sentences = re.split(r"(?<=[.!?])\s+", paragraph)
    pieces: list[str] = []
    current: list[str] = []
    current_tokens = 0

    for sentence in sentences:
        sentence_tokens = count_tokens(sentence)
        if current_tokens + sentence_tokens > max_tokens and current:
            pieces.append(" ".join(current))
            current = []
            current_tokens = 0
        current.append(sentence)
        current_tokens += sentence_tokens

    if current:
        pieces.append(" ".join(current))

    return pieces