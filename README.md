# AD.ReverseProxy

The AD.ReverseProxy library provides middleware to configure an in-process reverse proxy server.

## Current status

This project is experimental and ___not recommended for internet-facing servers___ at this time. 

## About

This project started to address a need to share a single port between several microservices.

While this is possible with the Http.Sys server, it is not supported by the Kestrel server without the use of a reverse proxy server such as IIS, Nginx, or Apache. This project aims to create a server-agnostic solution that would allow for a Kestrel-based reverse proxy.

## Design goals

The primary goal of the project is to develop lightweight middleware that allows any in-process server to _consider_ branching an incoming request to another server. Importantly, this branching should be configurable __throughout run time__.

An important secondary goal is to allow existing servers to incorporate this branching. That is, this middleware should not require its own server. Instead, an existing server should configure the middleware and then _dispatch_ as required. If no viable alternative is registered with the middleware, the host continues down the HTTP request pipeline.

The middleware is designed to be flexible by allowing forward registration by the  __host__, the initial recipient, or reverse registration by the  __client__, the subsequent recipient. Registration from the client requires that the host be configured to accept client-based registration. 

The middleware is also designed to allow for clients to be unregistered. This would support downtime-free microservice swapping. An updated microservice could be published, started, and registered with a host. Then the previous version can be unregistered. Until the server is unregistered, incoming requests will continue to the previous version as the targets are searched sequentially.

The middleware exposes endpoints for both _forward registration_ and _reverse registration_. These endpoints correspond to requests from host-to-client and client-to-host, respectively.

## Forward registration

Forward registration is configured from the __/register-forward__ endpoint on the host. This _forwards_ requests _of a specific pattern_ from the host to the client server. The endpoint accepts a query parameter __target__ which points to the client server.

## Reverse registration

Reverse registration is configured from the __/register-reverse__ endpoint on the client. This _reverses_ the normal pattern and asks a host to forward requests _of a specific pattern_ to the client server. The endpoint accepts a query parameter __target__ which points to the host server.
