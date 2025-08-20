# Use nginx to serve static files
FROM nginx:alpine

# Install apache2-utils for htpasswd
RUN apk add --no-cache apache2-utils

# Copy static files
COPY . /usr/share/nginx/html/

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Create entrypoint script to handle password generation
RUN echo '#!/bin/sh' > /docker-entrypoint-custom.sh && \
    echo 'if [ -n "$ADMIN_PASSWORD" ]; then' >> /docker-entrypoint-custom.sh && \
    echo '  htpasswd -cb /etc/nginx/.htpasswd admin "$ADMIN_PASSWORD"' >> /docker-entrypoint-custom.sh && \
    echo 'else' >> /docker-entrypoint-custom.sh && \
    echo '  htpasswd -cb /etc/nginx/.htpasswd admin "Password1"' >> /docker-entrypoint-custom.sh && \
    echo 'fi' >> /docker-entrypoint-custom.sh && \
    echo 'exec nginx -g "daemon off;"' >> /docker-entrypoint-custom.sh && \
    chmod +x /docker-entrypoint-custom.sh

# Expose port 80
EXPOSE 80

# Start nginx with custom entrypoint
CMD ["/docker-entrypoint-custom.sh"]
