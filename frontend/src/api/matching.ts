import { apiClient } from './client';

export interface MatchFactor {
  category: string;
  description: string;
  score: number;
  weight: number;
}

export interface JobRecommendation {
  jobId: string;
  title: string;
  companyName: string;
  companyLogoUrl?: string;
  location?: string;
  workLocationType?: string;
  employmentType?: string;
  salaryMin?: number;
  salaryMax?: number;
  salaryCurrency?: string;
  postedAt?: string;
  externalUrl?: string;
  matchScore: number;
  matchFactors: MatchFactor[];
}

export interface MatchingSummary {
  totalJobsAnalyzed: number;
  matchedJobs: number;
  averageScore: number;
  topMatchingSkills: string[];
  topMatchingTitles: string[];
}

export interface RecommendationsResponse {
  recommendations: JobRecommendation[];
  totalCount: number;
  page: number;
  pageSize: number;
  summary: MatchingSummary;
}

export interface RecommendationsRequest {
  pageSize?: number;
  page?: number;
  minScore?: number;
}

export const matchingApi = {
  getRecommendations: (request: RecommendationsRequest = {}): Promise<RecommendationsResponse> => {
    const params = new URLSearchParams();
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());
    if (request.page) params.append('page', request.page.toString());
    if (request.minScore !== undefined) params.append('minScore', request.minScore.toString());

    const queryString = params.toString();
    return apiClient.get(`/api/matching/recommendations${queryString ? `?${queryString}` : ''}`);
  },

  getJobScore: (jobId: string): Promise<JobRecommendation> =>
    apiClient.get(`/api/matching/jobs/${jobId}/score`),
};
