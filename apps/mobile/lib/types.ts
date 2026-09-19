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
    openAccidents?: number;
    overdueEquipment?: number;
    lostHoursLast12Months?: number;
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
  contentType?: string | null;
  fileSizeBytes?: number | null;
  photoCount?: number;
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

export type Entitlements = {
  status: string;
  plan?: string | null;
  planCode?: string | null;
  isActive: boolean;
  isPro?: boolean;
  features?: string[];
  trialEndsAt?: string | null;
  currentPeriodEnd?: string | null;
};

export type BillingSession = {
  url: string;
};

export type Photo = {
  id: string;
  ownerKind: string;
  ownerId: string;
  caption?: string | null;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  createdAt: string;
};

export type Accident = {
  id: string;
  occurredOn: string;
  location: string;
  severity: string;
  status: string;
  description: string;
  injuredPerson?: string | null;
  immediateAction?: string | null;
  lostHours: number;
  photoCount: number;
  createdAt: string;
  updatedAt: string;
};

export type LostHoursSummary = {
  totalHours: number;
  entries: LostHoursEntry[];
};

export type LostHoursEntry = {
  id: string;
  accidentId?: string | null;
  occurredOn: string;
  hours: number;
  reason: string;
  notes?: string | null;
  createdAt: string;
};

export type Equipment = {
  id: string;
  name: string;
  category: string;
  serialNumber?: string | null;
  calibrationDueOn?: string | null;
  serviceDueOn?: string | null;
  notes?: string | null;
  isOverdue: boolean;
  trafficLight: string;
  photoCount: number;
  createdAt: string;
  updatedAt: string;
};

export type QuestionnaireQuestion = {
  id: string;
  prompt: string;
  help: string;
  kind: string;
  required: boolean;
};

export type QuestionnaireTemplate = {
  schemeCode: string;
  schemeName: string;
  title: string;
  introduction: string;
  questionCount: number;
  latestResponseId?: string | null;
  latestGeneratedAt?: string | null;
  questions: QuestionnaireQuestion[];
};

export type QuestionnaireResponse = {
  id: string;
  schemeCode: string;
  schemeName: string;
  status: string;
  answers: Record<string, string | null>;
  generatedMarkdown?: string | null;
  createdAt: string;
  updatedAt: string;
};

