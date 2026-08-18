from fastapi import FastAPI

from Routers import chunking, embedding

app = FastAPI(
    title="NexusVault AI Service",
    description="Stateless AI inference service: chunking and embedding. ",
    version="0.1.0",
)

app.include_router(chunking.router)
app.include_router(embedding.router)


@app.get("/health")
def health():
    return {"status": "ok"}