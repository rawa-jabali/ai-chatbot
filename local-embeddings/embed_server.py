from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

app = FastAPI()

model = SentenceTransformer("intfloat/multilingual-e5-base")  # عربي + إنجليزي

class Req(BaseModel):
    text: str

@app.post("/embed")
def embed(req: Req):
    vec = model.encode(req.text, normalize_embeddings=True)
    return {"vector": vec.tolist(), "dim": len(vec)}
