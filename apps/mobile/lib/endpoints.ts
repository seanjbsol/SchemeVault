import { api, downloadAuthorizedFile, uploadFile, type UploadFile } from '@/lib/api';
import type {
  Accident,
  AuthResponse,
  BillingSession,
  Dashboard,
  Entitlements,
  Equipment,
  Evidence,
  LostHoursEntry,
  LostHoursSummary,
  Photo,
  QuestionnaireResponse,
  QuestionnaireTemplate,
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
  evidenceItem: (id: string) => api<Evidence>(`/api/evidence/${id}`),
  createEvidence: (body: { title: string; category: string; notes?: string; expiresOn?: string | null }) =>
    api<Evidence>('/api/evidence', { method: 'POST', body }),
  deleteEvidence: (id: string) => api<void>(`/api/evidence/${id}`, { method: 'DELETE' }),
  evidencePhotos: (id: string) => api<Photo[]>(`/api/evidence/${id}/photos`),
  uploadEvidencePhoto: (id: string, file: UploadFile) => uploadFile<Photo>(`/api/evidence/${id}/photos`, file),
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
  questionnaires: () => api<QuestionnaireTemplate[]>('/api/questionnaires'),
  questionnaire: (code: string) => api<QuestionnaireTemplate>(`/api/questionnaires/schemes/${code}`),
  saveQuestionnaire: (code: string, answers: Record<string, string>, generate = true) =>
    api<QuestionnaireResponse>(`/api/questionnaires/schemes/${code}/responses`, {
      method: 'POST',
      body: { answers, generate },
    }),
  questionnaireResponse: (id: string) => api<QuestionnaireResponse>(`/api/questionnaires/responses/${id}`),
  downloadQuestionnairePdf: (id: string, filename: string) =>
    downloadAuthorizedFile(`/api/questionnaires/responses/${id}/pdf`, filename),
  downloadExportPack: async (schemeCodes?: string[]) => {
    const { apiBaseUrl } = await import('@/lib/api');
    const { getToken } = await import('@/lib/tokenStorage');
    const token = await getToken();
    const response = await fetch(`${apiBaseUrl}/api/questionnaires/export`, {
      method: 'POST',
      headers: {
        Accept: 'application/pdf',
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({ schemeCodes }),
    });
    if (!response.ok) {
      const text = await response.text();
      let title = `Export failed (${response.status})`;
      try {
        const parsed = JSON.parse(text) as { title?: string };
        if (parsed.title) title = parsed.title;
      } catch {
        /* ignore */
      }
      const { ApiError } = await import('@/lib/api');
      throw new ApiError(title, response.status);
    }
    if (typeof document === 'undefined') {
      const { ApiError } = await import('@/lib/api');
      throw new ApiError('Pack export download is available in the Expo web app.', 0);
    }
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'schemevault-multi-scheme-pack.pdf';
    anchor.click();
    URL.revokeObjectURL(url);
  },
  accidents: () => api<Accident[]>('/api/accidents'),
  accident: (id: string) => api<Accident>(`/api/accidents/${id}`),
  createAccident: (body: {
    occurredOn: string;
    location: string;
    severity: string;
    status?: string;
    description: string;
    injuredPerson?: string;
    immediateAction?: string;
    lostHours?: number;
  }) => api<Accident>('/api/accidents', { method: 'POST', body }),
  deleteAccident: (id: string) => api<void>(`/api/accidents/${id}`, { method: 'DELETE' }),
  accidentPhotos: (id: string) => api<Photo[]>(`/api/accidents/${id}/photos`),
  uploadAccidentPhoto: (id: string, file: UploadFile) => uploadFile<Photo>(`/api/accidents/${id}/photos`, file),
  lostHours: () => api<LostHoursSummary>('/api/lost-hours'),
  addLostHours: (body: { occurredOn: string; hours: number; reason: string; accidentId?: string; notes?: string }) =>
    api<LostHoursEntry>('/api/lost-hours', { method: 'POST', body }),
  equipment: (overdue?: boolean) => api<Equipment[]>(overdue ? '/api/equipment?overdue=true' : '/api/equipment'),
  equipmentItem: (id: string) => api<Equipment>(`/api/equipment/${id}`),
  createEquipment: (body: {
    name: string;
    category: string;
    serialNumber?: string;
    calibrationDueOn?: string | null;
    serviceDueOn?: string | null;
    notes?: string;
  }) => api<Equipment>('/api/equipment', { method: 'POST', body }),
  deleteEquipment: (id: string) => api<void>(`/api/equipment/${id}`, { method: 'DELETE' }),
  equipmentPhotos: (id: string) => api<Photo[]>(`/api/equipment/${id}/photos`),
  uploadEquipmentPhoto: (id: string, file: UploadFile) => uploadFile<Photo>(`/api/equipment/${id}/photos`, file),
  deletePhoto: (id: string) => api<void>(`/api/photos/${id}`, { method: 'DELETE' }),
};
