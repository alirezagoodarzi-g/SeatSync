FROM node:22-alpine AS build
WORKDIR /app

COPY package.json ./
RUN npm install

COPY . .
ENV VITE_API_URL=http://localhost:8080/api
ENV VITE_SIGNALR_URL=http://localhost:8080/hubs/seatmap
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80