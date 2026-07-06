FROM nginx:alpine

# apply nginx routing config
COPY docker/nginx.conf /etc/nginx/nginx.conf

# Changing base href on env var
COPY docker/99-base-href.sh /docker-entrypoint.d/99-base-href.sh

RUN chmod +x /docker-entrypoint.d/99-base-href.sh

# Static files
COPY ./bin/Release/net8.0/publish/wwwroot /usr/share/nginx/html

EXPOSE 80