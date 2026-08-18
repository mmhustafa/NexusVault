from sentence_transformers import SentenceTransformer

# Loaded once, at module import time (i.e. once per process), not per-request.
# Cold-loading a transformer model on every request would be unusable --
# this is the same "load once, serve many" reasoning as any model-serving
# service.
_MODEL_NAME = "all-mpnet-base-v2"
_MODEL_VERSION = "1"  # bump manually if you ever change how this model is used/configured

_model = SentenceTransformer(_MODEL_NAME)


def embed_texts(texts: list[str]) -> tuple[list[list[float]], int]:
    """
    Batched embedding -- one call embeds every chunk of a document together,
    not one HTTP round-trip per chunk. Returns (vectors, dimensions).
    """
    vectors = _model.encode(texts, batch_size=32, show_progress_bar=False, convert_to_numpy=True)
    dimensions = vectors.shape[1]
    return vectors.tolist(), dimensions


def model_info() -> tuple[str, str]:
    return _MODEL_NAME, _MODEL_VERSION


def count_tokens(text: str) -> int:
    
    return len(_model.tokenizer.encode(text, add_special_tokens=False))


def max_sequence_length() -> int:
    """The embedding model's hard token limit (384 for all-mpnet-base-v2).
    Chunks should never be sized above this."""
    return _model.get_max_seq_length()