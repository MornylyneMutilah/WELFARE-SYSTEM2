FROM postgres:18
RUN apt-get update && apt-get install -y --no-install-recommends python3 ca-certificates \
    && rm -rf /var/lib/apt/lists/*
COPY transfer.py /opt/welfare/transfer.py
USER postgres
CMD ["python3", "/opt/welfare/transfer.py"]
