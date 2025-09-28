# Programmable API Monetization Gateway

## Overview
This project is a **Programmable API Monetization Gateway**, designed to sit between external API consumers and internal services. The system manages **authentication, access control, usage tracking, rate limiting, and billing** for API consumers.  

The gateway allows SaaS platforms to monetize APIs efficiently while providing detailed usage tracking and enforcing tiered service levels.

---

## Table of Contents
- [Background](#background)  
- [Features](#features)  
- [System Design](#system-design)  
- [Data Model](#data-model)  
- [Rate Limiting & Tier Enforcement](#rate-limiting--tier-enforcement)  
- [API Usage Tracking](#api-usage-tracking)  
- [Configuration](#configuration)  
- [How to Run](#how-to-run)  
- [Future Enhancements](#future-enhancements)  

---

## Background
Modern APIs are a core part of SaaS products. Monetizing APIs requires:  
- Controlling access per customer and tier.  
- Limiting usage via **rate limits** and **monthly quotas**.  
- Tracking usage for billing and analytics.  

This gateway addresses these requirements while allowing dynamic configuration of tiers and rate limits.

---

## Features
- **Tier-based access:** Free, Pro, or custom tiers.  
- **Rate limiting:** Enforces per-second limits and monthly quotas.  
- **API usage logging:** Captures customer ID, user ID, endpoint, timestamp, response status, and latency.  
- **Monthly summaries:** Backend job generates usage summaries and calculates billing.  
- **Configurable tiers:** Prices, quotas, and rate limits can be updated without redeploying.  

---

## System Design
### Architecture Diagram (ASCII/PlantUML style)

```text
 +----------------+      +------------------+      +--------------------+
 | External Users | ---> |   API Gateway    | ---> | Internal Services  |
 +----------------+      +------------------+      +--------------------+
                              |
                              v
                       +-----------------+
                       | Authentication  |
                       +-----------------+
                              |
                              v
                       +-----------------+
                       |  Rate Limiter   |
                       +-----------------+
                              |
                              v
                       +-----------------+
                       |  Usage Logger   |
                       +-----------------+
                              |
                              v
                       +-----------------+
                       | Billing & Summary|
                       +-----------------+
                              |
                              v
                          +--------+
                          | Database|
                          +--------+
**Components:**  
- **API Gateway:** Intercepts requests, validates API keys, applies rate limiting.  
- **Authentication Service:** Authenticates customers and users.  
- **Usage Logger:** Logs every successful API request.  
- **Rate Limiter:** Enforces tier-based limits and returns HTTP 429 on violations.  
- **Billing & Summary Service:** Aggregates monthly usage and calculates amount due.  
- **Database:** Stores customers, tiers, usage logs, and monthly summaries.  

---

## Data Model (ERD - ASCII style)

```text
+------------+       +--------+       +---------------+
|  Customer  |       |  Tier  |       |  UsageLog     |
+------------+       +--------+       +---------------+
| Id         |<----->| Id     |       | Id            |
| Name       |       | Name   |       | CustomerId    |
| TierId     |       | MonthlyQuota | | UserId       |
+------------+       | RateLimitPerSecond| Endpoint |
                     | PricePerMonth     | HttpMethod |
                     +-----------------+ | Timestamp  |
                                         | ResponseStatus |
                                         | LatencyMs      |
                                         +---------------+

+------------------+
| MonthlySummary   |
+------------------+
| Id               |
| CustomerId       |
| Year             |
| Month            |
| TotalRequests    |
| AmountDue        |
| GeneratedAt      |
+------------------+
## Rate Limiting & Tier Enforcement
- **Logic:** Requests are counted per customer. Each tier defines a **monthly quota** and a **per-second rate limit**.  
- **Violations:** Requests exceeding the limit return **HTTP 429 Too Many Requests**.  
- **Dynamic Configuration:** Tiers can be updated at runtime without code changes.  

**Example Pseudocode:**
```csharp
if (customerRequestsThisSecond >= tier.RateLimitPerSecond)
    return 429;

if (customerRequestsThisMonth >= tier.MonthlyQuota)
    return 429;
## API Usage Tracking
- Every successful request is logged with the following metadata:  
  - Customer ID  
  - Optional User ID  
  - Endpoint  
  - HTTP Method  
  - Timestamp  
  - Response Status  
  - Latency in milliseconds  

- **Monthly Summary Job:**  
  Aggregates usage logs by customer and month, calculates total requests, and computes the amount due based on tier pricing.  

---

## Configuration
- Tiers can be defined in the database or via a configuration file.  
- Supports adding new tiers or updating limits dynamically without redeploying.  

**Example Configuration:**
```json
[
  { "Name": "Free", "MonthlyQuota": 100, "RateLimitPerSecond": 2, "PricePerMonth": 0 },
  { "Name": "Pro", "MonthlyQuota": 100000, "RateLimitPerSecond": 10, "PricePerMonth": 50 }
]
