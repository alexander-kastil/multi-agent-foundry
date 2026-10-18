FROM python:3.14-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY main.py workflow_main.py ./
EXPOSE 8088
CMD ["python", "main.py"]
