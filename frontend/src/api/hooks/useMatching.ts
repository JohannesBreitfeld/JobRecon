import { useQuery } from '@tanstack/react-query';
import { matchingApi } from '../matching';
import type { RecommendationsRequest } from '../matching';
import { queryKeys } from './queryKeys';

export function useRecommendations(params: RecommendationsRequest) {
  return useQuery({
    queryKey: queryKeys.matching.recommendations(params),
    queryFn: () => matchingApi.getRecommendations(params),
  });
}
