import { useState, useEffect } from 'react';
import {
  Box,
  TextField,
  Button,
  Grid,
  Typography,
  Chip,
  IconButton,
  InputAdornment,
} from '@mui/material';
import { Add as AddIcon, Close as CloseIcon } from '@mui/icons-material';
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
    desiredJobTitles: [],
  });

  const [newJobTitle, setNewJobTitle] = useState('');

  // Sync form state when profile loads asynchronously
  useEffect(() => {
    if (profile) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFormData({
        currentJobTitle: profile.currentJobTitle || '',
        summary: profile.summary || '',
        location: profile.location || '',
        yearsOfExperience: profile.yearsOfExperience,
        desiredJobTitles: profile.desiredJobTitles || [],
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

  const handleAddJobTitle = () => {
    if (newJobTitle.trim() && !formData.desiredJobTitles?.includes(newJobTitle.trim())) {
      setFormData((prev) => ({
        ...prev,
        desiredJobTitles: [...(prev.desiredJobTitles || []), newJobTitle.trim()],
      }));
      setNewJobTitle('');
    }
  };

  const handleRemoveJobTitle = (title: string) => {
    setFormData((prev) => ({
      ...prev,
      desiredJobTitles: prev.desiredJobTitles?.filter((t) => t !== title) || [],
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
          <Typography variant="subtitle2" gutterBottom>
            Önskade jobbtitlar
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
            Dessa titlar används för att matcha dig mot relevanta jobb.
          </Typography>
          <TextField
            fullWidth
            label="Lägg till jobbtitel"
            value={newJobTitle}
            onChange={(e) => setNewJobTitle(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                handleAddJobTitle();
              }
            }}
            disabled={isLoading}
            slotProps={{
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton onClick={handleAddJobTitle} disabled={isLoading || !newJobTitle.trim()}>
                      <AddIcon />
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }}
          />
          <Box sx={{ mt: 1, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {formData.desiredJobTitles?.map((title) => (
              <Chip
                key={title}
                label={title}
                onDelete={() => handleRemoveJobTitle(title)}
                deleteIcon={<CloseIcon />}
                disabled={isLoading}
              />
            ))}
          </Box>
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
