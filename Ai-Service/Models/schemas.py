from pydantic import BaseModel, Field

class ChunkRequest(BaseModel):
    text: str = Field(..., description="Full extracted document text.")
    max_tokens_per_chunk: int = Field(default=300, ge=50, le=1000)


class ChunkItem(BaseModel):
    chunk_index: int
    text: str
    page_number: int | None = None
    section_heading: str | None = None


class ChunkResponse(BaseModel):
    chunks: list[ChunkItem]



class EmbedRequest(BaseModel):
    texts: list[str] = Field(..., min_length=1)


class EmbedResponse(BaseModel):
    vectors: list[list[float]]
    model_name: str
    model_version: str
    dimensions: int