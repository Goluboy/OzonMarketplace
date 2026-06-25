import Keycloak from 'keycloak-js';

const keycloakConfig = {
  url: 'https://d5ddimrn36v2tdv58n65.nkhmighe.apigw.yandexcloud.net/auth',
  realm: process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'marketplace',
  clientId: process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'marketplace-app',
};

const keycloak = new Keycloak(keycloakConfig);

export default keycloak
