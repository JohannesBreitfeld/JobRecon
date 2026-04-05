import { useState, useEffect, useCallback } from 'react';
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
  Autocomplete,
  IconButton,
  Paper,
  Stack,
  Chip,
} from '@mui/material';
import { Add as AddIcon, Close as CloseIcon } from '@mui/icons-material';
import DeleteIcon from '@mui/icons-material/Delete';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import Tooltip from '@mui/material/Tooltip';
import { useProfile, useUpdatePreferences, useUpdateProfile } from '../../api/hooks/useProfile';
import { jobsApi, type LocalityResponse } from '../../api/jobs';
import type {
  EmploymentType,
  UpdateJobPreferenceRequest,
  PreferredLocationRequest,
} from '../../api/profile';

const employmentTypeLabels: Record<EmploymentType, string> = {
  FullTime: 'Heltid',
  PartTime: 'Deltid',
  Contract: 'Kontrakt',
  Freelance: 'Frilans',
  Internship: 'Praktik',
};

export function PreferencesSection() {
  const { data: profile } = useProfile();
  const updatePreferencesMutation = useUpdatePreferences();
  const updateProfileMutation = useUpdateProfile();

  const isLoading = updatePreferencesMutation.isPending || updateProfileMutation.isPending;

  const initialPref = profile?.jobPreference;

  const [formData, setFormData] = useState<Omit<UpdateJobPreferenceRequest, 'preferredLocations'>>(() => {
    if (initialPref) {
      return {
        minSalary: initialPref.minSalary,
        maxSalary: initialPref.maxSalary,
        isRemotePreferred: initialPref.isRemotePreferred,
        isHybridAccepted: initialPref.isHybridAccepted,
        isOnSiteAccepted: initialPref.isOnSiteAccepted,
        preferredEmploymentTypes: initialPref.preferredEmploymentTypes,
        excludedCompanies: initialPref.excludedCompanies || '',
        isActivelyLooking: initialPref.isActivelyLooking,
      };
    }
    return {
      minSalary: undefined,
      maxSalary: undefined,
      isRemotePreferred: false,
      isHybridAccepted: true,
      isOnSiteAccepted: true,
      preferredEmploymentTypes: 'FullTime',
      excludedCompanies: '',
      isActivelyLooking: true,
    };
  });

  const [preferredLocations, setPreferredLocations] = useState<PreferredLocationRequest[]>(() => {
    if (initialPref) {
      return initialPref.preferredLocations.map((l) => ({
        localityId: l.localityId,
        name: l.name,
        latitude: l.latitude,
        longitude: l.longitude,
        maxDistanceKm: l.maxDistanceKm,
      }));
    }
    return [];
  });

  const [desiredJobTitles, setDesiredJobTitles] = useState<string[]>(
    () => profile?.desiredJobTitles || []
  );
  const [newJobTitle, setNewJobTitle] = useState('');

  const [localityOptions, setLocalityOptions] = useState<LocalityResponse[]>([]);
  const [localityInputValue, setLocalityInputValue] = useState('');


  const handleSearchLocalities = useCallback(async (query: string) => {
    if (query.length < 2) {
      setLocalityOptions([]);
      return;
    }
    try {
      const results = await jobsApi.searchLocalities(query);
      setLocalityOptions(results);
    } catch {
      setLocalityOptions([]);
    }
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      handleSearchLocalities(localityInputValue);
    }, 300);
    return () => clearTimeout(timer);
  }, [localityInputValue, handleSearchLocalities]);

  const handleAddLocality = (_: unknown, value: LocalityResponse | null) => {
    if (!value) return;
    if (preferredLocations.some((l) => l.localityId === value.geoNameId)) return;

    setPreferredLocations((prev) => [
      ...prev,
      {
        localityId: value.geoNameId,
        name: value.name,
        latitude: value.latitude,
        longitude: value.longitude,
        maxDistanceKm: undefined,
      },
    ]);
    setLocalityInputValue('');
    setLocalityOptions([]);
  };

  const handleRemoveLocation = (localityId: number) => {
    setPreferredLocations((prev) => prev.filter((l) => l.localityId !== localityId));
  };

  const handleMaxDistanceChange = (localityId: number, value: string) => {
    setPreferredLocations((prev) =>
      prev.map((l) =>
        l.localityId === localityId
          ? { ...l, maxDistanceKm: value ? parseInt(value, 10) : undefined }
          : l
      )
    );
  };

  const handleAddJobTitle = () => {
    if (newJobTitle.trim() && !desiredJobTitles.includes(newJobTitle.trim())) {
      setDesiredJobTitles((prev) => [...prev, newJobTitle.trim()]);
      setNewJobTitle('');
    }
  };

  const handleRemoveJobTitle = (title: string) => {
    setDesiredJobTitles((prev) => prev.filter((t) => t !== title));
  };

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
    updatePreferencesMutation.mutate({
      ...formData,
      preferredLocations,
    });
    updateProfileMutation.mutate({ desiredJobTitles });
  };

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Grid container spacing={3}>
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
            onKeyDown={(e) => {
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
            {desiredJobTitles.map((title) => (
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
              name="preferredEmploymentTypes"
              value={formData.preferredEmploymentTypes}
              label="Önskad anställningstyp"
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  preferredEmploymentTypes: e.target.value as EmploymentType,
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
          <Typography variant="subtitle2" gutterBottom>
            Föredragna platser
          </Typography>

          <Autocomplete
            options={localityOptions}
            getOptionLabel={(option) => `${option.name} (${option.population.toLocaleString('sv-SE')} inv.)`}
            filterOptions={(x) => x}
            value={null}
            inputValue={localityInputValue}
            onInputChange={(_, value) => setLocalityInputValue(value)}
            onChange={handleAddLocality}
            noOptionsText={localityInputValue.length < 2 ? 'Skriv minst 2 tecken...' : 'Inga resultat'}
            disabled={isLoading}
            renderInput={(params) => (
              <TextField {...params} label="Sök och lägg till ort" placeholder="t.ex. Stockholm" />
            )}
            isOptionEqualToValue={(option, value) => option.geoNameId === value.geoNameId}
            blurOnSelect
            clearOnBlur
            sx={{ mb: 2 }}
          />

          {preferredLocations.length > 0 && (
            <Stack spacing={1}>
              {preferredLocations.map((loc) => (
                <Paper key={loc.localityId} variant="outlined" sx={{ p: 1.5 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <Typography sx={{ fontWeight: 500, minWidth: 120 }}>{loc.name}</Typography>
                    <TextField
                      size="small"
                      type="number"
                      label="Max avstånd"
                      value={loc.maxDistanceKm || ''}
                      onChange={(e) => handleMaxDistanceChange(loc.localityId, e.target.value)}
                      disabled={isLoading}
                      placeholder="Exakt matchning"
                      slotProps={{
                        input: {
                          endAdornment: <InputAdornment position="end">km</InputAdornment>,
                        },
                        htmlInput: { min: 1, max: 500 },
                      }}
                      sx={{ width: 200 }}
                    />
                    <Typography variant="caption" color="text.secondary" sx={{ flex: 1 }}>
                      {loc.maxDistanceKm
                        ? `Jobb inom ${loc.maxDistanceKm} km`
                        : 'Endast exakt matchning'}
                    </Typography>
                    <IconButton
                      size="small"
                      onClick={() => handleRemoveLocation(loc.localityId)}
                      disabled={isLoading}
                      aria-label={`Ta bort ${loc.name}`}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Box>
                </Paper>
              ))}
            </Stack>
          )}
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.5 }}>
            <Typography variant="subtitle2">
              Uteslutna företag
            </Typography>
            <Tooltip title="Företag som du inte vill matchas med visas inte i dina rekommendationer" arrow>
              <InfoOutlinedIcon fontSize="small" color="action" sx={{ cursor: 'help' }} />
            </Tooltip>
          </Box>
          <TextField
            fullWidth
            name="excludedCompanies"
            value={formData.excludedCompanies}
            onChange={handleChange}
            disabled={isLoading}
            placeholder="Företag du inte vill matcha med"
            helperText="Separera med komma"
          />
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Button type="submit" variant="contained" color="secondary" disabled={isLoading} sx={{ mt: 2 }}>
            {isLoading ? 'Sparar...' : 'Spara preferenser'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  );
}
