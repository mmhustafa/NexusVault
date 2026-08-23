from sentence_transformers import CrossEncoder

_MODEL_NAME = "cross-encoder/ms-marco-MiniLM-L-12-v2"
_model = CrossEncoder(_MODEL_NAME)

def rerank(query: str, candidates: list[tuple[str, str]]) -> list[tuple[str, float]]:
    """
    candidates: list of (id, text) pairs.
    Returns: list of (id, score) pairs, NOT sorted here -- sorting is the
    caller's responsibility (keeps this function a pure scoring step).
    """
    if not candidates:
        return []

    pairs = [(query, text) for _id, text in candidates]
    scores = _model.predict(pairs)

    return [(candidate_id, float(score)) for (candidate_id, _text), score in zip(candidates, scores)]


def model_name() -> str:
    return _MODEL_NAME