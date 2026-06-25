FROM nginx:alpine

# apply nginx routing config
COPY nginx.conf /etc/nginx/nginx.conf

WORKDIR /usr/share/nginx/html

COPY ./bin/Release/net8.0/publish/wwwroot .

EXPOSE 80