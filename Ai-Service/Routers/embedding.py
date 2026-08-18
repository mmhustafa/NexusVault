from fastapi import APIRouter

from Models.schemas import EmbedRequest, EmbedResponse
from Services.embedder import embed_texts, model_info

router = APIRouter()


@router.post("/embed", response_model=EmbedResponse)
def embed(request: EmbedRequest) -> EmbedResponse:
    vectors, dimensions = embed_texts(request.texts)
    model_name, model_version = model_info()
    return EmbedResponse(
        vectors=vectors,
        model_name=model_name,
        model_version=model_version,
        dimensions=dimensions,
    )