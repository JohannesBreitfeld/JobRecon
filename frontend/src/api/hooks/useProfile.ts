import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { profileApi } from '../profile';
import type {
  CreateProfileRequest,
  UpdateProfileRequest,
  AddSkillRequest,
  UpdateJobPreferenceRequest,
} from '../profile';
import { ApiError } from '../client';
import { queryKeys } from './queryKeys';

export function useProfile() {
  return useQuery({
    queryKey: queryKeys.profile.current(),
    queryFn: () => profileApi.getProfile(),
    retry: (failureCount, error) => {
      if (error instanceof ApiError && error.code === 'Profile.NotFound') return false;
      return failureCount < 1;
    },
  });
}

export function useCreateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateProfileRequest) => profileApi.createProfile(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateProfileRequest) => profileApi.updateProfile(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useAddSkill() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: AddSkillRequest) => profileApi.addSkill(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useRemoveSkill() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (skillId: string) => profileApi.removeSkill(skillId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateJobPreferenceRequest) => profileApi.updatePreferences(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useUploadCV() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => profileApi.uploadCV(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useDeleteCV() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => profileApi.deleteCV(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}

export function useSetPrimaryCV() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => profileApi.setPrimaryCV(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.profile.all });
    },
  });
}
