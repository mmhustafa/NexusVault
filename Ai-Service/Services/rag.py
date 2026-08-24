import json
import os

from groq import Groq

_client: Groq | None = None


def _get_client() -> Groq:
    global _client
    if _client is None:
        api_key = os.environ.get("GROQ_API_KEY")
        if not api_key:
            raise RuntimeError(
                "GROQ_API_KEY environment variable is not set. "
                "Set it before starting the ai-service (never commit it to source control)."
            )
        _client = Groq(api_key=api_key)
    return _client


_MODEL = os.environ.get("GROQ_MODEL", "llama-3.3-70b-versatile")

_SYSTEM_PROMPT = """You are a grounded question-answering assistant. You will be given a user question and a set of labeled source excerpts.

Rules:
- Answer ONLY using information contained in the provided excerpts.
- If the excerpts do not contain enough information to answer the question, say so explicitly instead of guessing or using outside knowledge.
- Respond ONLY with a JSON object, no other text before or after it, in exactly this shape:
  {"answer": "<your answer text>", "cited_chunk_ids": ["<id of each excerpt you actually used>"]}
- cited_chunk_ids must only contain ids that were given to you below -- never invent an id, and never cite an excerpt you didn't actually use.
"""


def synthesize(query: str, chunks: list[tuple[str, str]]) -> tuple[str, list[str]]:
    """
    chunks: list of (id, text) pairs, already retrieved+reranked upstream --
    this function does no retrieval of its own.
    Returns: (answer, cited_chunk_ids).
    """
    numbered = "\n\n".join(f"[{chunk_id}]\n{text}" for chunk_id, text in chunks)
    user_prompt = f"Question: {query}\n\nSource excerpts:\n{numbered}"

    response = _get_client().chat.completions.create(
        model=_MODEL,
        messages=[
            {"role": "system", "content": _SYSTEM_PROMPT},
            {"role": "user", "content": user_prompt},
        ],
        temperature=0.0,  # deterministic, appropriate for grounded QA -- this
                          # isn't creative writing, answers should be reproducible
        response_format={"type": "json_object"},
    )

    raw = response.choices[0].message.content

    try:
        parsed = json.loads(raw)
        answer = parsed.get("answer", "")
        cited = parsed.get("cited_chunk_ids", [])
        if not isinstance(cited, list):
            cited = []
        return answer, [str(c) for c in cited]
    except (json.JSONDecodeError, AttributeError, TypeError):
        return raw or "", []