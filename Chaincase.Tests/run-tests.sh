docker-compose -f "../WalletWasabi.SDK/WalletWasabi.Backend/docker-compose.yml" -f "docker-compose.yml" down -v
docker-compose -f "../WalletWasabi.SDK/WalletWasabi.Backend/docker-compose.yml" -f "docker-compose.yml" pull
docker-compose -f "docker-compose.yml" -f "../WalletWasabi.SDK/WalletWasabi.Backend/docker-compose.yml"  build
docker-compose -f "docker-compose.yml" -f "../WalletWasabi.SDK/WalletWasabi.Backend/docker-compose.yml"  run tests

