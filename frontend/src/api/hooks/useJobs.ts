import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { jobsApi } from '../jobs';
import type { JobSearchRequest, SavedJobStatus } from '../jobs';
import { queryKeys } from './queryKeys';

export function useJobSearch(params: JobSearchRequest) {
  return useQuery({
    queryKey: queryKeys.jobs.search(params),
    queryFn: () => jobsApi.searchJobs(params),
  });
}

export function useJob(id: string | null) {
  return useQuery({
    queryKey: queryKeys.jobs.detail(id!),
    queryFn: () => jobsApi.getJob(id!),
    enabled: !!id,
  });
}

export function useSavedJobs() {
  return useQuery({
    queryKey: queryKeys.jobs.saved(),
    queryFn: () => jobsApi.getSavedJobs(),
  });
}

export function useJobStatistics() {
  return useQuery({
    queryKey: queryKeys.jobs.statistics(),
    queryFn: () => jobsApi.getStatistics(),
  });
}

export function useSaveJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ jobId, notes }: { jobId: string; notes?: string }) =>
      jobsApi.saveJob(jobId, { notes }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.saved() });
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all });
    },
  });
}

export function useUpdateSavedJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ jobId, status, notes }: { jobId: string; status: SavedJobStatus; notes?: string }) =>
      jobsApi.updateSavedJob(jobId, { status, notes }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.saved() });
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all });
    },
  });
}

export function useRemoveSavedJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId: string) => jobsApi.removeSavedJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.saved() });
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all });
    },
  });
}
