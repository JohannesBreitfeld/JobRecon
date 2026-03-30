import { useState } from 'react';
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
} from '@mui/material';
import { Search as SearchIcon, FilterList as FilterIcon, Clear as ClearIcon } from '@mui/icons-material';
import { useJobsStore } from '../../stores/jobsStore';
import type { WorkLocationType, EmploymentType } from '../../api/jobs';

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

  const handleSearch = () => {
    setSearchParams({ query: localQuery, location: localLocation, page: 1 });
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const handleClearFilters = () => {
    setLocalQuery('');
    setLocalLocation('');
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
          <TextField
            fullWidth
            placeholder="Sök jobb, företag eller kompetenser..."
            value={localQuery}
            onChange={(e) => setLocalQuery(e.target.value)}
            onKeyPress={handleKeyPress}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
                endAdornment: localQuery && (
                  <InputAdornment position="end">
                    <IconButton size="small" onClick={() => setLocalQuery('')}>
                      <ClearIcon fontSize="small" />
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }}
          />
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <TextField
            fullWidth
            placeholder="Plats..."
            value={localLocation}
            onChange={(e) => setLocalLocation(e.target.value)}
            onKeyPress={handleKeyPress}
          />
        </Grid>

        <Grid size={{ xs: 6, md: 1.5 }}>
          <Button
            fullWidth
            variant="contained"
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
