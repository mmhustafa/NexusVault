from fastapi import APIRouter

from Models.schemas import RerankRequest, RerankResponse, RerankedItem
from Services.reranker import rerank

router = APIRouter()


@router.post("/rerank", response_model=RerankResponse)
def rerank_candidates(request: RerankRequest) -> RerankResponse:
    pairs = [(c.id, c.text) for c in request.candidates]
    scored = rerank(request.query, pairs)

    # Sorting happens here, not in the reranker module -- keeps rerank() a
    # pure scoring function, and the endpoint owns the "what order does the
    # caller receive results in" decision.
    ranked = sorted(scored, key=lambda pair: pair[1], reverse=True)

    return RerankResponse(ranked=[RerankedItem(id=id_, score=score) for id_, score in ranked])