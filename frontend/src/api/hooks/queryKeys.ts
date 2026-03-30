import type { JobSearchRequest } from '../jobs';
import type { RecommendationsRequest } from '../matching';

export const queryKeys = {
  jobs: {
    all: ['jobs'] as const,
    search: (params: JobSearchRequest) => ['jobs', 'search', params] as const,
    detail: (id: string) => ['jobs', 'detail', id] as const,
    saved: () => ['jobs', 'saved'] as const,
    statistics: () => ['jobs', 'statistics'] as const,
  },
  profile: {
    all: ['profile'] as const,
    current: () => ['profile', 'current'] as const,
  },
  matching: {
    all: ['matching'] as const,
    recommendations: (params: RecommendationsRequest) => ['matching', 'recommendations', params] as const,
  },
};
