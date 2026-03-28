import { useState } from 'react';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
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
  InputAdornment,
  IconButton,
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { authApi } from '../api/auth';

const resetPasswordSchema = z.object({
  email: z.string().email('Ogiltig e-postadress'),
  token: z.string().min(1, 'Token krävs'),
  newPassword: z
    .string()
    .min(6, 'Lösenordet måste vara minst 6 tecken')
    .regex(/[A-Z]/, 'Lösenordet måste innehålla minst en stor bokstav')
    .regex(/[a-z]/, 'Lösenordet måste innehålla minst en liten bokstav')
    .regex(/[0-9]/, 'Lösenordet måste innehålla minst en siffra')
    .regex(/[^A-Za-z0-9]/, 'Lösenordet måste innehålla minst ett specialtecken'),
  confirmPassword: z.string(),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: 'Lösenorden matchar inte',
  path: ['confirmPassword'],
});

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: searchParams.get('email') ?? '',
      token: searchParams.get('token') ?? '',
    },
  });

  const onSubmit = async (data: ResetPasswordFormData) => {
    setIsLoading(true);
    setError(null);
    try {
      await authApi.resetPassword({
        email: data.email,
        token: data.token,
        newPassword: data.newPassword,
      });
      setSuccess(true);
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
            Återställ lösenord
          </Typography>

          {success ? (
            <>
              <Alert severity="success" sx={{ mb: 2 }}>
                Ditt lösenord har återställts.
              </Alert>
              <Box sx={{ textAlign: 'center' }}>
                <Link component={RouterLink} to="/login">
                  Logga in med ditt nya lösenord
                </Link>
              </Box>
            </>
          ) : (
            <>
              <Typography variant="body2" color="text.secondary" align="center" sx={{ mb: 3 }}>
                Ange ditt nya lösenord nedan.
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
                  error={!!errors.email}
                  helperText={errors.email?.message}
                  sx={{ mb: 2 }}
                />

                <TextField
                  {...register('token')}
                  label="Återställningstoken"
                  fullWidth
                  error={!!errors.token}
                  helperText={errors.token?.message}
                  sx={{ mb: 2 }}
                />

                <TextField
                  {...register('newPassword')}
                  label="Nytt lösenord"
                  type={showPassword ? 'text' : 'password'}
                  fullWidth
                  autoComplete="new-password"
                  error={!!errors.newPassword}
                  helperText={errors.newPassword?.message}
                  sx={{ mb: 2 }}
                  slotProps={{
                    input: {
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={() => setShowPassword(!showPassword)}
                            edge="end"
                          >
                            {showPassword ? <VisibilityOff /> : <Visibility />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    },
                  }}
                />

                <TextField
                  {...register('confirmPassword')}
                  label="Bekräfta lösenord"
                  type={showPassword ? 'text' : 'password'}
                  fullWidth
                  autoComplete="new-password"
                  error={!!errors.confirmPassword}
                  helperText={errors.confirmPassword?.message}
                  sx={{ mb: 3 }}
                />

                <Button
                  type="submit"
                  variant="contained"
                  fullWidth
                  size="large"
                  disabled={isLoading}
                >
                  {isLoading ? <CircularProgress size={24} /> : 'Återställ lösenord'}
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
