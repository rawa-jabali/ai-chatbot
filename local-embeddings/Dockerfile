FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY embed_server.py .

EXPOSE 8001

CMD ["uvicorn", "embed_server:app", "--host", "0.0.0.0", "--port", "8001"]
