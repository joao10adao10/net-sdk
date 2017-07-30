# Kubernetes Ingress Samples

These samples show how to deploy the YARP Kubernetes Ingress Controller into a Kubernetes cluster.

An [Ingress Controller](https://kubernetes.io/docs/concepts/services-networking/ingress-controllers/) monitors for [Ingress resources](https://kubernetes.io/docs/concepts/services-networking/ingress/) and routes traffic to services.

There are three parts to these samples:
- [Backend](./backend/README.md)
- [Combined Ingress Controller](./Combined/README.md)
- [Separate Ingress Controller and Monitor](./Ingress/README.md)

The "Backend" is a Dockerized ASP.NET Core application that returns dummy information in web requests. This project contains Kubernetes manifest files for deploying the application and an Ingress resource into a cluster.

The Ingress Controller can be deployed either as:
- a single deployable (see the Combined sample), or
- as two separate deployables where one (the "monitor") watches the Ingress resources and the other (the "ingress") retrieves the YARP configuration from the "monitor" and handles the routing

Both of these controllers utilize the `Yarp.Kubernetes.Controller` project.

## Ingress Resource

The `Yarp.Kubernetes.Controller` project currently supports the following Ingress features:

- [Ingress rules](https://kubernetes.io/docs/concepts/services-networking/ingress/#ingress-rules) for host name and path-based routing to backend services
- [Ingress class](https://kubernetes.io/docs/concepts/services-networking/ingress/#ingress-class) for multiple, independent instances of the controller (cluster scope only)
- [Default ingress class](https://kubernetes.io/docs/concepts/services-networking/ingress/#default-ingress-class) for simplifying Ingress resource configuration

The `Yarp.Kubernetes.Controller` project does not support:
- The [TLS specification](https://kubernetes.io/docs/concepts/services-networking/ingress/#tls) for Ingress resources (coming soon), though you could combined with the LetsEncrypt.Sample.
- The [deprecated annotation](https://kubernetes.io/docs/concepts/services-networking/ingress/#deprecated-annotation) for ingress resources.

### Annotations

The `Yarp.Kubernetes.Controller` project supports a number of **optional** annotations on Ingress resources for functionality provided by YARP.

These annotations would be specified like this:
```
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: minimal-ingress
  namespace: default
  annotations:
    yarp.ingress.kubernetes.io/authorization-policy: authzpolicy
    yarp.ingress.kubernetes.io/transforms: |
      - PathRemovePrefix: "/apis"
spec:
  rules:
    - http:
        paths:
          - path: /foo
            pathType: Prefix
            backend:
              service:
                name: frontend
                port:
                  number: 80
```

The table below lists the available annotations.

|Annotation|Data Type|
|---|---|
|yarp.ingress.kubernetes.io/authorization-policy|string|
|yarp.ingress.kubernetes.io/backend-protocol|string|
|yarp.ingress.kubernetes.io/cors-policy|string|
|yarp.ingress.kubernetes.io/health-check|[ActivateHealthCheckConfig](https://microsoft.github.io/reverse-proxy/api/Yarp.ReverseProxy.Configuration.ActiveHealthCheckConfig.html)|
|yarp.ingress.kubernetes.io/http-client|[HttpClientConfig](https://microsoft.github.io/reverse-proxy/api/Yarp.ReverseProxy.Configuration.HttpClientConfig.html)|
|yarp.ingress.kubernetes.io/load-balancing|string|
|yarp.ingress.kubernetes.io/route-metadata|Dictionary<string, string>|
|yarp.ingress.kubernetes.io/session-affinity|[SessionAffinityConfig](https://microsoft.github.io/reverse-proxy/api/Yarp.ReverseProxy.Configuration.SessionAffinityConfig.html)|
|yarp.ingress.kubernetes.io/transforms|List<Dictionary<string, string>>|

#### Authorization Policy

See https://microsoft.github.io/reverse-proxy/articles/authn-authz.html for a list of available policies, or how to add your own custom policies.

`yarp.ingress.kubernetes.io/authorization-policy: anonymous`

#### Backend Protocol

Specifies the protocol of the backend service. Defaults to http.

`yarp.ingress.kubernetes.io/backend-protocol: "https"`

#### CORS Policy

See https://microsoft.github.io/reverse-proxy/articles/cors.html for the list of available policies, or how to add your own custom policies.

`yarp.ingress.kubernetes.io/cors-policy: mypolicy`

#### Health Check

Proactively monitors destination health by sending periodic probing requests to designated health endpoints and analyzing responses.

See https://microsoft.github.io/reverse-proxy/articles/dests-health-checks.html.

```
yarp.ingress.kubernetes.io/health-check |
  Active:
  Enabled: true
  Interval: '00:00:10'
  Timeout: '00:00:10'
  Policy: ConsecutiveFailures
  Path: "/api/health"
```

#### HTTP Client

Configures the HTTP client that will be used for the destination service.

See https://microsoft.github.io/reverse-proxy/articles/http-client-config.html.

```
yarp.ingress.kubernetes.io/http-client: |
  SslProtocols: Ssl3
  MaxConnectionsPerServer: 2
  DangerousAcceptAnyServerCertificate: true
```

#### Load Balancing

See https://microsoft.github.io/reverse-proxy/articles/load-balancing.html for a list of the available options.

`yarp.ingress.kubernetes.io/load-balancing: Random`

#### Route Metadata

See https://microsoft.github.io/reverse-proxy/api/Yarp.ReverseProxy.Configuration.RouteConfig.html#Yarp_ReverseProxy_Configuration_RouteConfig_Metadata.

```
yarp.ingress.kubernetes.io/route-metadata: |
  - Custom: "orange"
  - Tenant: "12345"
```

#### Session Affinity

See https://microsoft.github.io/reverse-proxy/articles/session-affinity.html.

```
yarp.ingress.kubernetes.io/session-affinity: |
  Enabled: true
  Policy: Cookie
  FailurePolicy: Redistribute
  AffinityKeyName: Key1
  Cookie:
    Domain: localhost
    Expiration:
    HttpOnly: true
    IsEssential: true
    MaxAge:
    Path: mypath
    SameSite: Strict
    SecurePolicy: Always
```

#### Transforms

Transforms use the YAML key-value pairs as per the YARP [Request Transforms](https://microsoft.github.io/reverse-proxy/articles/transforms.html#request-transforms)

```
yarp.ingress.kubernetes.io/transforms: |
  - PathPrefix: "/apis"
  - RequestHeader: header1
    Append: bar
```
