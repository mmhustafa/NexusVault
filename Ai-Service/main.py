
from dotenv import load_dotenv

load_dotenv()

from fastapi import FastAPI

from Routers import chunking, embedding, reranking, rag

app = FastAPI(
    title="NexusVault AI Service",
    description="Stateless AI inference service: chunking and embedding. ",
    version="0.1.0",
)

app.include_router(chunking.router)
app.include_router(embedding.router)
app.include_router(reranking.router)
app.include_router(rag.router)

@app.get("/health")
def health():
    return {"status": "ok"}