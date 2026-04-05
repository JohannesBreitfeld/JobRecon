import { useState, useEffect } from 'react';
import {
  Box,
  TextField,
  Button,
  Grid,
} from '@mui/material';
import { useProfile, useUpdateProfile } from '../../api/hooks/useProfile';
import type { UpdateProfileRequest } from '../../api/profile';

export function ProfileForm() {
  const { data: profile } = useProfile();
  const updateProfileMutation = useUpdateProfile();

  const [formData, setFormData] = useState<UpdateProfileRequest>({
    currentJobTitle: '',
    summary: '',
    location: '',
    yearsOfExperience: undefined,
  });

  // Sync form state when profile loads asynchronously
  useEffect(() => {
    if (profile) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFormData({
        currentJobTitle: profile.currentJobTitle || '',
        summary: profile.summary || '',
        location: profile.location || '',
        yearsOfExperience: profile.yearsOfExperience,
      });
    }
  }, [profile]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'yearsOfExperience' ? (value ? parseInt(value, 10) : undefined) : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    updateProfileMutation.mutate(formData);
  };

  const isLoading = updateProfileMutation.isPending;

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            fullWidth
            label="Nuvarande jobbtitel"
            name="currentJobTitle"
            value={formData.currentJobTitle}
            onChange={handleChange}
            disabled={isLoading}
          />
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            fullWidth
            label="Plats"
            name="location"
            value={formData.location}
            onChange={handleChange}
            disabled={isLoading}
          />
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            fullWidth
            label="År av erfarenhet"
            name="yearsOfExperience"
            type="number"
            value={formData.yearsOfExperience || ''}
            onChange={handleChange}
            disabled={isLoading}
            slotProps={{ htmlInput: { min: 0, max: 50 } }}
          />
        </Grid>

        <Grid size={{ xs: 12 }}>
          <TextField
            fullWidth
            label="Sammanfattning"
            name="summary"
            value={formData.summary}
            onChange={handleChange}
            disabled={isLoading}
            multiline
            rows={4}
            placeholder="Beskriv dig själv och din erfarenhet..."
          />
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Button
            type="submit"
            variant="contained"
            color="secondary"
            disabled={isLoading}
            sx={{ mt: 2 }}
          >
            {isLoading ? 'Sparar...' : 'Spara ändringar'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  );
}
