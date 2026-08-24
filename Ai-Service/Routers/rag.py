from fastapi import APIRouter

from Models.schemas import RagSynthesizeRequest, RagSynthesizeResponse
from Services.rag import synthesize

router = APIRouter()


@router.post("/rag-synthesize", response_model=RagSynthesizeResponse)
def rag_synthesize(request: RagSynthesizeRequest) -> RagSynthesizeResponse:
    chunks = [(c.id, c.text) for c in request.chunks]
    answer, cited_ids = synthesize(request.query, chunks)
    return RagSynthesizeResponse(answer=answer, cited_chunk_ids=cited_ids)