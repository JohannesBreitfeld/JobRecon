import { apiClient } from './client';

export type SkillLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';
export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Freelance' | 'Internship';

export interface SkillResponse {
  id: string;
  name: string;
  level: SkillLevel;
  yearsOfExperience?: number;
}

export interface JobPreferenceResponse {
  id: string;
  minSalary?: number;
  maxSalary?: number;
  preferredLocations?: string;
  isRemotePreferred: boolean;
  isHybridAccepted: boolean;
  isOnSiteAccepted: boolean;
  preferredEmploymentType: EmploymentType;
  preferredIndustries?: string;
  excludedCompanies?: string;
  isActivelyLooking: boolean;
  availableFrom?: string;
  noticePeriodDays?: number;
}

export interface CVDocumentResponse {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  isPrimary: boolean;
  isParsed: boolean;
  uploadedAt: string;
}

export interface ProfileResponse {
  id: string;
  userId: string;
  currentJobTitle?: string;
  summary?: string;
  location?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  portfolioUrl?: string;
  yearsOfExperience?: number;
  desiredJobTitles: string[];
  skills: SkillResponse[];
  jobPreference?: JobPreferenceResponse;
  cvDocuments: CVDocumentResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateProfileRequest {
  currentJobTitle?: string;
  summary?: string;
  location?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  portfolioUrl?: string;
  yearsOfExperience?: number;
  desiredJobTitles?: string[];
}

export interface UpdateProfileRequest {
  currentJobTitle?: string;
  summary?: string;
  location?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  portfolioUrl?: string;
  yearsOfExperience?: number;
  desiredJobTitles?: string[];
}

export interface AddSkillRequest {
  name: string;
  level: SkillLevel;
  yearsOfExperience?: number;
}

export interface UpdateJobPreferenceRequest {
  minSalary?: number;
  maxSalary?: number;
  preferredLocations?: string;
  isRemotePreferred: boolean;
  isHybridAccepted: boolean;
  isOnSiteAccepted: boolean;
  preferredEmploymentType: EmploymentType;
  preferredIndustries?: string;
  excludedCompanies?: string;
  isActivelyLooking: boolean;
  availableFrom?: string;
  noticePeriodDays?: number;
}

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const profileApi = {
  getProfile: (): Promise<ProfileResponse> =>
    apiClient.get('/api/profile'),

  createProfile: (request: CreateProfileRequest): Promise<ProfileResponse> =>
    apiClient.post('/api/profile', request),

  updateProfile: (request: UpdateProfileRequest): Promise<ProfileResponse> =>
    apiClient.put('/api/profile', request),

  addSkill: (request: AddSkillRequest): Promise<SkillResponse> =>
    apiClient.post('/api/profile/skills', request),

  removeSkill: (skillId: string): Promise<void> =>
    apiClient.delete(`/api/profile/skills/${skillId}`),

  getPreferences: (): Promise<JobPreferenceResponse> =>
    apiClient.get('/api/profile/preferences'),

  updatePreferences: (request: UpdateJobPreferenceRequest): Promise<JobPreferenceResponse> =>
    apiClient.put('/api/profile/preferences', request),

  uploadCV: async (file: File): Promise<CVDocumentResponse> => {
    const formData = new FormData();
    formData.append('file', file);

    const token = localStorage.getItem('accessToken');
    const response = await fetch(`${API_BASE_URL}/api/profile/cv`, {
      method: 'POST',
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: formData,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Ett fel uppstod' }));
      throw new Error(error.message || error.error || 'Ett fel uppstod vid uppladdning');
    }

    return response.json();
  },

  downloadCV: (documentId: string): string =>
    `${API_BASE_URL}/api/profile/cv/${documentId}`,

  deleteCV: (documentId: string): Promise<void> =>
    apiClient.delete(`/api/profile/cv/${documentId}`),

  setPrimaryCV: (documentId: string): Promise<void> =>
    apiClient.post(`/api/profile/cv/${documentId}/primary`),
};
