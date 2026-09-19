import { api } from '@/lib/api';
import type {
  AuthResponse,
  BillingSession,
  Dashboard,
  Entitlements,
  Evidence,
  Renewal,
  SchemeDetail,
  SchemeSummary,
  Tenant,
  User,
} from '@/lib/types';

export const endpoints = {
  login: (email: string, password: string) =>
    api<AuthResponse>('/api/auth/login', { method: 'POST', auth: false, body: { email, password } }),
  register: (payload: { organisationName: string; fullName: string; email: string; password: string }) =>
    api<AuthResponse>('/api/auth/register', { method: 'POST', auth: false, body: payload }),
  me: () => api<User>('/api/auth/me'),
  dashboard: () => api<Dashboard>('/api/dashboard'),
  schemes: () => api<SchemeSummary[]>('/api/schemes'),
  scheme: (id: string) => api<SchemeDetail>(`/api/schemes/${id}`),
  evidence: () => api<Evidence[]>('/api/evidence'),
  createEvidence: (body: { title: string; category: string; notes?: string; expiresOn?: string | null }) =>
    api<Evidence>('/api/evidence', { method: 'POST', body }),
  deleteEvidence: (id: string) => api<void>(`/api/evidence/${id}`, { method: 'DELETE' }),
  renewals: () => api<Renewal[]>('/api/renewals'),
  createRenewal: (body: {
    schemeId: string;
    status: string;
    expiresOn?: string | null;
    membershipNumber?: string;
    notes?: string;
  }) => api<Renewal>('/api/renewals', { method: 'POST', body }),
  tenant: () => api<Tenant>('/api/tenants/current'),
  updateTenant: (name: string) => api<Tenant>('/api/tenants/current', { method: 'PATCH', body: { name } }),
  entitlements: () => api<Entitlements>('/api/billing/entitlements'),
  billingCheckout: (body: { successUrl: string; cancelUrl: string }) =>
    api<BillingSession>('/api/billing/checkout', { method: 'POST', body }),
  billingPortal: (body: { returnUrl: string }) =>
    api<BillingSession>('/api/billing/portal', { method: 'POST', body }),
};
