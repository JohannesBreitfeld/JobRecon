import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Container,
  Paper,
  Typography,
  Link,
  Box,
  TextField,
  Button,
  Alert,
  CircularProgress,
} from '@mui/material';
import { authApi } from '../api/auth';

const forgotPasswordSchema = z.object({
  email: z.string().email('Ogiltig e-postadress'),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export function ForgotPasswordPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setIsLoading(true);
    setError(null);
    try {
      await authApi.forgotPassword({ email: data.email });
      setSubmitted(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ett ovantat fel uppstod.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Container maxWidth="sm">
      <Box
        sx={{
          mt: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Paper elevation={3} sx={{ p: 4, width: '100%' }}>
          <Typography component="h1" variant="h4" align="center" gutterBottom>
            Glömt lösenord
          </Typography>

          {submitted ? (
            <>
              <Alert severity="success" sx={{ mb: 2 }}>
                Om ett konto med den e-postadressen finns har vi skickat instruktioner för att återställa ditt lösenord.
              </Alert>
              <Box sx={{ textAlign: 'center' }}>
                <Link component={RouterLink} to="/login">
                  Tillbaka till inloggning
                </Link>
              </Box>
            </>
          ) : (
            <>
              <Typography variant="body2" color="text.secondary" align="center" sx={{ mb: 3 }}>
                Ange din e-postadress så skickar vi en länk för att återställa ditt lösenord.
              </Typography>

              <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
                {error && (
                  <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                  </Alert>
                )}

                <TextField
                  {...register('email')}
                  label="E-post"
                  type="email"
                  fullWidth
                  autoComplete="email"
                  autoFocus
                  error={!!errors.email}
                  helperText={errors.email?.message}
                  sx={{ mb: 3 }}
                />

                <Button
                  type="submit"
                  variant="contained"
                  fullWidth
                  size="large"
                  disabled={isLoading}
                >
                  {isLoading ? <CircularProgress size={24} /> : 'Skicka återställningslänk'}
                </Button>
              </Box>

              <Box sx={{ mt: 3, textAlign: 'center' }}>
                <Link component={RouterLink} to="/login">
                  Tillbaka till inloggning
                </Link>
              </Box>
            </>
          )}
        </Paper>
      </Box>
    </Container>
  );
}
