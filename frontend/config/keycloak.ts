import Keycloak from 'keycloak-js';

const keycloakConfig = {
  url: process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost/auth',
  realm: process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'marketplace',
  clientId: process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'marketplace-app',
};

const keycloak = new Keycloak(keycloakConfig);

export default keycloak;