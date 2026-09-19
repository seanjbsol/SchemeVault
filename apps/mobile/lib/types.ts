export type User = {
  id: string;
  email: string;
  fullName: string;
  tenantId: string;
  tenantName: string;
  role: 'Owner' | 'Admin' | 'Member' | string;
};

export type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: User;
};

export type Dashboard = {
  tenantName: string;
  counts: {
    upcoming: number;
    expired: number;
    missingEvidence: number;
    activeSchemes: number;
    evidenceItems: number;
  };
  upcomingRenewals: Renewal[];
  expiredRenewals: Renewal[];
  missingEvidence: GapItem[];
};

export type Renewal = {
  id: string;
  schemeId: string;
  schemeCode: string;
  schemeName: string;
  status: string;
  expiresOn?: string | null;
  lastSubmittedOn?: string | null;
  membershipNumber?: string | null;
  notes?: string | null;
  trafficLight: string;
  daysUntilExpiry?: number | null;
};

export type Evidence = {
  id: string;
  title: string;
  category: string;
  notes?: string | null;
  expiresOn?: string | null;
  trafficLight: string;
  hasFile: boolean;
  originalFileName?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type GapItem = {
  id: string;
  schemeId: string;
  title: string;
  description?: string | null;
  status: string;
  evidenceItemId?: string | null;
  evidenceTitle?: string | null;
};

export type SchemeSummary = {
  id: string;
  code: string;
  name: string;
  provider: string;
  isSsipStyle: boolean;
  accreditation?: Renewal | null;
  missingGaps: number;
  completeGaps: number;
};

export type SchemeDetail = {
  id: string;
  code: string;
  name: string;
  provider: string;
  description: string;
  isSsipStyle: boolean;
  accreditation?: Renewal | null;
  gaps: GapItem[];
};

export type Tenant = {
  id: string;
  name: string;
};
