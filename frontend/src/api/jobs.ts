import { apiClient } from './client';

export type WorkLocationType = 'OnSite' | 'Remote' | 'Hybrid';
export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Freelance' | 'Internship' | 'Temporary';
export type JobStatus = 'Active' | 'Expired' | 'Filled' | 'Removed';
export type SavedJobStatus = 'Saved' | 'Applied' | 'Interviewing' | 'Rejected' | 'Offered' | 'Accepted' | 'Withdrawn';
export type JobSourceType = 'Manual' | 'Arbetsformedlingen' | 'LinkedIn' | 'Indeed' | 'Glassdoor' | 'CustomRss' | 'ApiIntegration';

export interface CompanyResponse {
  id: string;
  name: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  industry?: string;
  location?: string;
  employeeCount?: number;
  jobCount: number;
}

export interface JobListResponse {
  id: string;
  title: string;
  location?: string;
  workLocationType?: WorkLocationType;
  employmentType?: EmploymentType;
  salaryMin?: number;
  salaryMax?: number;
  salaryCurrency?: string;
  postedAt?: string;
  companyName: string;
  companyLogoUrl?: string;
  isSaved: boolean;
  savedStatus?: SavedJobStatus;
}

export interface JobResponse {
  id: string;
  title: string;
  description?: string;
  location?: string;
  workLocationType?: WorkLocationType;
  employmentType?: EmploymentType;
  salaryMin?: number;
  salaryMax?: number;
  salaryCurrency?: string;
  salaryPeriod?: string;
  externalUrl?: string;
  applicationUrl?: string;
  requiredSkills?: string;
  benefits?: string;
  experienceYearsMin?: number;
  experienceYearsMax?: number;
  postedAt?: string;
  expiresAt?: string;
  status: JobStatus;
  company: CompanyResponse;
  sourceName: string;
  tags: string[];
  isSaved: boolean;
  savedStatus?: SavedJobStatus;
  createdAt: string;
}

export interface JobSearchRequest {
  query?: string;
  location?: string;
  workLocationType?: WorkLocationType;
  employmentType?: EmploymentType;
  salaryMin?: number;
  salaryMax?: number;
  companyId?: string;
  jobSourceId?: string;
  experienceYearsMax?: number;
  tags?: string[];
  savedOnly?: boolean;
  postedAfter?: string;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface JobSearchResponse {
  jobs: JobListResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SavedJobResponse {
  id: string;
  status: SavedJobStatus;
  notes?: string;
  appliedAt?: string;
  savedAt: string;
  job: JobListResponse;
}

export interface JobStatisticsResponse {
  totalJobs: number;
  activeJobs: number;
  newJobsToday: number;
  newJobsThisWeek: number;
  savedJobsCount: number;
  jobsBySource: Record<string, number>;
  jobsByLocation: Record<string, number>;
  jobsByType: Record<string, number>;
}

export interface SaveJobRequest {
  notes?: string;
}

export interface UpdateSavedJobRequest {
  status: SavedJobStatus;
  notes?: string;
  appliedAt?: string;
}

export interface JobSourceResponse {
  id: string;
  name: string;
  type: JobSourceType;
  isEnabled: boolean;
  fetchIntervalMinutes: number;
  lastFetchedAt?: string;
  lastFetchJobCount?: number;
  lastFetchError?: string;
}

export const jobsApi = {
  searchJobs: (request: JobSearchRequest = {}): Promise<JobSearchResponse> => {
    const params = new URLSearchParams();
    if (request.query) params.append('query', request.query);
    if (request.location) params.append('location', request.location);
    if (request.workLocationType) params.append('workLocationType', request.workLocationType);
    if (request.employmentType) params.append('employmentType', request.employmentType);
    if (request.salaryMin) params.append('salaryMin', request.salaryMin.toString());
    if (request.salaryMax) params.append('salaryMax', request.salaryMax.toString());
    if (request.companyId) params.append('companyId', request.companyId);
    if (request.jobSourceId) params.append('jobSourceId', request.jobSourceId);
    if (request.experienceYearsMax) params.append('experienceYearsMax', request.experienceYearsMax.toString());
    if (request.savedOnly !== undefined) params.append('savedOnly', request.savedOnly.toString());
    if (request.postedAfter) params.append('postedAfter', request.postedAfter);
    if (request.sortBy) params.append('sortBy', request.sortBy);
    if (request.sortDescending !== undefined) params.append('sortDescending', request.sortDescending.toString());
    if (request.page) params.append('page', request.page.toString());
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());

    const queryString = params.toString();
    return apiClient.get(`/api/jobs${queryString ? `?${queryString}` : ''}`);
  },

  getJob: (id: string): Promise<JobResponse> =>
    apiClient.get(`/api/jobs/${id}`),

  getStatistics: (): Promise<JobStatisticsResponse> =>
    apiClient.get('/api/jobs/statistics'),

  getCompanies: (search?: string, limit: number = 20): Promise<CompanyResponse[]> => {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    params.append('limit', limit.toString());
    return apiClient.get(`/api/jobs/companies?${params.toString()}`);
  },

  getCompany: (id: string): Promise<CompanyResponse> =>
    apiClient.get(`/api/jobs/companies/${id}`),

  getSavedJobs: (): Promise<SavedJobResponse[]> =>
    apiClient.get('/api/jobs/saved'),

  saveJob: (jobId: string, request: SaveJobRequest = {}): Promise<SavedJobResponse> =>
    apiClient.post(`/api/jobs/saved/${jobId}`, request),

  updateSavedJob: (jobId: string, request: UpdateSavedJobRequest): Promise<SavedJobResponse> =>
    apiClient.put(`/api/jobs/saved/${jobId}`, request),

  removeSavedJob: (jobId: string): Promise<void> =>
    apiClient.delete(`/api/jobs/saved/${jobId}`),

  getJobSources: (): Promise<JobSourceResponse[]> =>
    apiClient.get('/api/jobs/sources'),
};
