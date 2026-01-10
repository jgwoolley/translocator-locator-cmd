VS_SERVER_URL="https://cdn.vintagestory.at/gamefiles/stable/vs_client_linux-x64_1.21.6.tar.gz"
mkdir -p vs_server
wget -O vs_server.tar.gz $VS_SERVER_URL
tar -xzf vs_server.tar.gz -C vs_server --strip-components=1
rm vs_server.tar.gz