FROM nginx:alpine

# apply nginx routing config
COPY nginx.conf /etc/nginx/nginx.conf

COPY ./bin/Release/net8.0/publish/wwwroot /usr/share/nginx/html

EXPOSE 80