from fastapi import APIRouter

from Models.schemas import ChunkRequest, ChunkResponse
from Services.chunker import chunk_text

router = APIRouter()


@router.post("/chunk", response_model=ChunkResponse)
def chunk(request: ChunkRequest) -> ChunkResponse:
    chunks = chunk_text(request.text, request.max_tokens_per_chunk)
    return ChunkResponse(chunks=chunks)