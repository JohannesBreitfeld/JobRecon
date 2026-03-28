import { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  TextField,
  Button,
  Grid,
  FormControlLabel,
  Switch,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  InputAdornment,
} from '@mui/material';
import { useProfileStore } from '../../stores/profileStore';
import type { EmploymentType, UpdateJobPreferenceRequest } from '../../api/profile';

const employmentTypeLabels: Record<EmploymentType, string> = {
  FullTime: 'Heltid',
  PartTime: 'Deltid',
  Contract: 'Kontrakt',
  Freelance: 'Frilans',
  Internship: 'Praktik',
};

export function PreferencesSection() {
  const { profile, updatePreferences, isLoading } = useProfileStore();

  const [formData, setFormData] = useState<UpdateJobPreferenceRequest>({
    minSalary: undefined,
    maxSalary: undefined,
    preferredLocations: '',
    isRemotePreferred: false,
    isHybridAccepted: true,
    isOnSiteAccepted: true,
    preferredEmploymentType: 'FullTime',
    excludedCompanies: '',
  });

  useEffect(() => {
    if (profile?.jobPreference) {
      const pref = profile.jobPreference;
      setFormData({
        minSalary: pref.minSalary,
        maxSalary: pref.maxSalary,
        preferredLocations: pref.preferredLocations || '',
        isRemotePreferred: pref.isRemotePreferred,
        isHybridAccepted: pref.isHybridAccepted,
        isOnSiteAccepted: pref.isOnSiteAccepted,
        preferredEmploymentType: pref.preferredEmploymentType,
        excludedCompanies: pref.excludedCompanies || '',
      });
    }
  }, [profile]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        type === 'checkbox'
          ? checked
          : type === 'number'
            ? value
              ? parseInt(value, 10)
              : undefined
            : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await updatePreferences(formData);
  };

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Typography variant="h6" gutterBottom>
        Jobbpreferenser
      </Typography>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            fullWidth
            label="Minimilön"
            name="minSalary"
            type="number"
            value={formData.minSalary || ''}
            onChange={handleChange}
            disabled={isLoading}
            slotProps={{
              input: {
                startAdornment: <InputAdornment position="start">SEK</InputAdornment>,
              },
              htmlInput: { min: 0 },
            }}
          />
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            fullWidth
            label="Maxlön"
            name="maxSalary"
            type="number"
            value={formData.maxSalary || ''}
            onChange={handleChange}
            disabled={isLoading}
            slotProps={{
              input: {
                startAdornment: <InputAdornment position="start">SEK</InputAdornment>,
              },
              htmlInput: { min: 0 },
            }}
          />
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <FormControl fullWidth>
            <InputLabel>Önskad anställningstyp</InputLabel>
            <Select
              name="preferredEmploymentType"
              value={formData.preferredEmploymentType}
              label="Önskad anställningstyp"
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  preferredEmploymentType: e.target.value as EmploymentType,
                }))
              }
              disabled={isLoading}
            >
              {Object.entries(employmentTypeLabels).map(([value, label]) => (
                <MenuItem key={value} value={value}>
                  {label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Typography variant="subtitle2" gutterBottom>
            Arbetsplats
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            <FormControlLabel
              control={
                <Switch
                  name="isRemotePreferred"
                  checked={formData.isRemotePreferred}
                  onChange={handleChange}
                  disabled={isLoading}
                />
              }
              label="Distans föredras"
            />
            <FormControlLabel
              control={
                <Switch
                  name="isHybridAccepted"
                  checked={formData.isHybridAccepted}
                  onChange={handleChange}
                  disabled={isLoading}
                />
              }
              label="Hybrid accepteras"
            />
            <FormControlLabel
              control={
                <Switch
                  name="isOnSiteAccepted"
                  checked={formData.isOnSiteAccepted}
                  onChange={handleChange}
                  disabled={isLoading}
                />
              }
              label="På plats accepteras"
            />
          </Box>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <TextField
            fullWidth
            label="Föredragna platser"
            name="preferredLocations"
            value={formData.preferredLocations}
            onChange={handleChange}
            disabled={isLoading}
            placeholder="t.ex. Stockholm, Göteborg, Malmö"
            helperText="Separera med komma"
          />
        </Grid>

        <Grid size={{ xs: 12 }}>
          <TextField
            fullWidth
            label="Uteslutna företag"
            name="excludedCompanies"
            value={formData.excludedCompanies}
            onChange={handleChange}
            disabled={isLoading}
            placeholder="Företag du inte vill matcha med"
            helperText="Separera med komma"
          />
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Button type="submit" variant="contained" disabled={isLoading} sx={{ mt: 2 }}>
            {isLoading ? 'Sparar...' : 'Spara preferenser'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  );
}
