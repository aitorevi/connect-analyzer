# Unprivileged nginx: runs as UID 101 and listens on 8080 (no root, no <1024 bind).
FROM nginxinc/nginx-unprivileged:alpine
USER root
COPY data/ /usr/share/nginx/html/
USER 101
