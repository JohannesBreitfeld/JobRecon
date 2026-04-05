import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Button,
  Grid,
  Collapse,
  InputAdornment,
  IconButton,
  Autocomplete,
} from '@mui/material';
import { Search as SearchIcon, FilterList as FilterIcon, Clear as ClearIcon } from '@mui/icons-material';
import { useJobsStore } from '../../stores/jobsStore';
import { jobsApi } from '../../api/jobs';
import type { WorkLocationType, EmploymentType, LocalityResponse } from '../../api/jobs';

const workLocationLabels: Record<WorkLocationType, string> = {
  OnSite: 'På plats',
  Remote: 'Distans',
  Hybrid: 'Hybrid',
};

const employmentTypeLabels: Record<EmploymentType, string> = {
  FullTime: 'Heltid',
  PartTime: 'Deltid',
  Contract: 'Kontrakt',
  Freelance: 'Frilans',
  Internship: 'Praktik',
  Temporary: 'Tidsbegränsad',
};

export function JobSearchFilters() {
  const { searchParams, setSearchParams } = useJobsStore();
  const [showFilters, setShowFilters] = useState(false);
  const [localQuery, setLocalQuery] = useState(searchParams.query || '');
  const [localLocation, setLocalLocation] = useState(searchParams.location || '');
  const [suggestions, setSuggestions] = useState<string[]>([]);
  const [loadingSuggestions, setLoadingSuggestions] = useState(false);
  const [locationOptions, setLocationOptions] = useState<LocalityResponse[]>([]);
  const [locationInputValue, setLocationInputValue] = useState(searchParams.location || '');
  const [loadingLocations, setLoadingLocations] = useState(false);

  const fetchSuggestions = useCallback((input: string) => {
    if (!input || input.length < 2) {
      setSuggestions([]);
      return;
    }
    setLoadingSuggestions(true);
    jobsApi.getTags(input, 10).then((tags) => {
      setSuggestions(tags);
      setLoadingSuggestions(false);
    }).catch(() => {
      setLoadingSuggestions(false);
    });
  }, []);

  const fetchLocations = useCallback((input: string) => {
    if (!input || input.length < 2) {
      setLocationOptions([]);
      return;
    }
    setLoadingLocations(true);
    jobsApi.searchLocalities(input, 20).then((results) => {
      setLocationOptions(results);
      setLoadingLocations(false);
    }).catch(() => {
      setLoadingLocations(false);
    });
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => fetchSuggestions(localQuery), 300);
    return () => clearTimeout(timer);
  }, [localQuery, fetchSuggestions]);

  useEffect(() => {
    const timer = setTimeout(() => fetchLocations(locationInputValue), 300);
    return () => clearTimeout(timer);
  }, [locationInputValue, fetchLocations]);

  const handleSearch = () => {
    setSearchParams({ query: localQuery, location: localLocation, page: 1 });
  };

  const handleClearFilters = () => {
    setLocalQuery('');
    setLocalLocation('');
    setLocationInputValue('');
    setSearchParams({
      query: undefined,
      location: undefined,
      workLocationType: undefined,
      employmentType: undefined,
      salaryMin: undefined,
      salaryMax: undefined,
      page: 1,
      pageSize: 20,
      sortBy: 'date',
      sortDescending: true,
    });
  };

  return (
    <Box sx={{ mb: 3 }}>
      <Grid container spacing={2} alignItems="center">
        <Grid size={{ xs: 12, md: 5 }}>
          <Autocomplete
            freeSolo
            options={suggestions}
            loading={loadingSuggestions}
            inputValue={localQuery}
            onInputChange={(_, value, reason) => {
              if (reason !== 'reset') setLocalQuery(value);
            }}
            onChange={(_, value) => {
              if (typeof value === 'string') {
                setLocalQuery(value);
                setSearchParams({ query: value, location: localLocation, page: 1 });
              }
            }}
            filterOptions={(x) => x}
            renderInput={(params) => (
              <TextField
                {...params}
                placeholder="Sök jobb, företag eller kompetenser..."
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    handleSearch();
                  }
                }}
                slotProps={{
                  input: {
                    ...params.InputProps,
                    startAdornment: (
                      <>
                        <InputAdornment position="start">
                          <SearchIcon />
                        </InputAdornment>
                        {params.InputProps.startAdornment}
                      </>
                    ),
                    endAdornment: (
                      <>
                        {localQuery && (
                          <InputAdornment position="end">
                            <IconButton size="small" onClick={() => setLocalQuery('')} aria-label="Rensa söktext">
                              <ClearIcon fontSize="small" />
                            </IconButton>
                          </InputAdornment>
                        )}
                        {params.InputProps.endAdornment}
                      </>
                    ),
                  },
                }}
              />
            )}
          />
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Autocomplete
            freeSolo
            options={locationOptions}
            getOptionLabel={(option) =>
              typeof option === 'string'
                ? option
                : `${option.name} (${option.population.toLocaleString('sv-SE')} inv.)`
            }
            filterOptions={(x) => x}
            loading={loadingLocations}
            inputValue={locationInputValue}
            onInputChange={(_, value, reason) => {
              if (reason !== 'reset') {
                setLocationInputValue(value);
                setLocalLocation(value);
              }
            }}
            onChange={(_, value) => {
              if (value && typeof value !== 'string') {
                setLocalLocation(value.name);
                setLocationInputValue(value.name);
                setSearchParams({ query: localQuery, location: value.name, page: 1 });
              } else if (typeof value === 'string') {
                setLocalLocation(value);
                setLocationInputValue(value);
              }
            }}
            noOptionsText={locationInputValue.length < 2 ? 'Skriv minst 2 tecken...' : 'Inga resultat'}
            renderInput={(params) => (
              <TextField
                {...params}
                placeholder="Plats..."
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    handleSearch();
                  }
                }}
              />
            )}
          />
        </Grid>

        <Grid size={{ xs: 6, md: 1.5 }}>
          <Button
            fullWidth
            variant="contained"
            color="secondary"
            onClick={handleSearch}
            sx={{ height: 56 }}
          >
            Sök
          </Button>
        </Grid>

        <Grid size={{ xs: 6, md: 1.5 }}>
          <Button
            fullWidth
            variant="outlined"
            onClick={() => setShowFilters(!showFilters)}
            startIcon={<FilterIcon />}
            sx={{ height: 56 }}
          >
            Filter
          </Button>
        </Grid>
      </Grid>

      <Collapse in={showFilters}>
        <Box sx={{ mt: 2, p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Arbetsplats</InputLabel>
                <Select
                  value={searchParams.workLocationType || ''}
                  label="Arbetsplats"
                  onChange={(e) => setSearchParams({ workLocationType: e.target.value as WorkLocationType || undefined })}
                >
                  <MenuItem value="">Alla</MenuItem>
                  {Object.entries(workLocationLabels).map(([value, label]) => (
                    <MenuItem key={value} value={value}>
                      {label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Anställningstyp</InputLabel>
                <Select
                  value={searchParams.employmentType || ''}
                  label="Anställningstyp"
                  onChange={(e) => setSearchParams({ employmentType: e.target.value as EmploymentType || undefined })}
                >
                  <MenuItem value="">Alla</MenuItem>
                  {Object.entries(employmentTypeLabels).map(([value, label]) => (
                    <MenuItem key={value} value={value}>
                      {label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid size={{ xs: 6, sm: 3, md: 2 }}>
              <TextField
                fullWidth
                size="small"
                label="Min lön"
                type="number"
                value={searchParams.salaryMin || ''}
                onChange={(e) => setSearchParams({ salaryMin: e.target.value ? Number(e.target.value) : undefined })}
                slotProps={{
                  input: {
                    endAdornment: <InputAdornment position="end">kr</InputAdornment>,
                  },
                }}
              />
            </Grid>

            <Grid size={{ xs: 6, sm: 3, md: 2 }}>
              <TextField
                fullWidth
                size="small"
                label="Max lön"
                type="number"
                value={searchParams.salaryMax || ''}
                onChange={(e) => setSearchParams({ salaryMax: e.target.value ? Number(e.target.value) : undefined })}
                slotProps={{
                  input: {
                    endAdornment: <InputAdornment position="end">kr</InputAdornment>,
                  },
                }}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
              <Button fullWidth variant="text" onClick={handleClearFilters}>
                Rensa filter
              </Button>
            </Grid>
          </Grid>
        </Box>
      </Collapse>
    </Box>
  );
}
