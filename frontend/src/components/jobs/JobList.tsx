import {
  Box,
  Grid,
  Typography,
  Pagination,
  CircularProgress,
  Alert,
  FormControl,
  Select,
  MenuItem,
  InputLabel,
} from '@mui/material';
import { useJobsStore } from '../../stores/jobsStore';
import { useJobSearch } from '../../api/hooks/useJobs';
import { JobCard } from './JobCard';

interface JobListProps {
  onJobClick: (jobId: string) => void;
}

export function JobList({ onJobClick }: JobListProps) {
  const { searchParams, setSearchParams } = useJobsStore();
  const { data: searchResults, isLoading, error } = useJobSearch(searchParams);

  const handlePageChange = (_: unknown, page: number) => {
    setSearchParams({ page });
  };

  const handleSortChange = (sortBy: string) => {
    setSearchParams({ sortBy, page: 1 });
  };

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        {error instanceof Error ? error.message : 'Ett fel uppstod vid sökning'}
      </Alert>
    );
  }

  if (isLoading && !searchResults) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!searchResults || searchResults.jobs.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">
          Inga jobb hittades. Prova att ändra dina sökkriterier.
        </Typography>
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="body2" color="text.secondary">
          {searchResults.totalCount} jobb hittades
        </Typography>

        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Sortera efter</InputLabel>
          <Select
            value={searchParams.sortBy || 'date'}
            label="Sortera efter"
            onChange={(e) => handleSortChange(e.target.value)}
          >
            <MenuItem value="date">Senaste först</MenuItem>
            <MenuItem value="salary">Lön</MenuItem>
            <MenuItem value="company">Företag</MenuItem>
            <MenuItem value="title">Titel</MenuItem>
          </Select>
        </FormControl>
      </Box>

      <Grid container spacing={2}>
        {searchResults.jobs.map((job) => (
          <Grid size={{ xs: 12, md: 6 }} key={job.id}>
            <JobCard job={job} onClick={() => onJobClick(job.id)} />
          </Grid>
        ))}
      </Grid>

      {searchResults.totalPages > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
          <Pagination
            count={searchResults.totalPages}
            page={searchResults.page}
            onChange={handlePageChange}
            color="primary"
            disabled={isLoading}
          />
        </Box>
      )}
    </Box>
  );
}
