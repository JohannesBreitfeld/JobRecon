import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Box,
  Button,
  TextField,
  Alert,
  CircularProgress,
  InputAdornment,
  IconButton,
  Stack,
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { useAuthStore } from '../../stores/authStore';

const registerSchema = z.object({
  email: z.string().email('Ogiltig e-postadress'),
  password: z
    .string()
    .min(6, 'Lösenordet måste vara minst 6 tecken')
    .regex(/[A-Z]/, 'Lösenordet måste innehålla minst en stor bokstav')
    .regex(/[a-z]/, 'Lösenordet måste innehålla minst en liten bokstav')
    .regex(/[0-9]/, 'Lösenordet måste innehålla minst en siffra')
    .regex(/[^A-Za-z0-9]/, 'Lösenordet måste innehålla minst ett specialtecken'),
  confirmPassword: z.string(),
  firstName: z.string().optional(),
  lastName: z.string().optional(),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Lösenorden matchar inte',
  path: ['confirmPassword'],
});

type RegisterFormData = z.infer<typeof registerSchema>;

interface RegisterFormProps {
  onSuccess?: () => void;
}

export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const [showPassword, setShowPassword] = useState(false);
  const { register: registerUser, isLoading, error, clearError } = useAuthStore();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    try {
      await registerUser({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
      });
      onSuccess?.();
    } catch {
      // Error is handled by the store
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={clearError}>
          {error}
        </Alert>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          {...register('firstName')}
          label="Förnamn"
          fullWidth
          autoComplete="given-name"
          autoFocus
          error={!!errors.firstName}
          helperText={errors.firstName?.message}
        />
        <TextField
          {...register('lastName')}
          label="Efternamn"
          fullWidth
          autoComplete="family-name"
          error={!!errors.lastName}
          helperText={errors.lastName?.message}
        />
      </Stack>

      <TextField
        {...register('email')}
        label="E-post"
        type="email"
        fullWidth
        autoComplete="email"
        error={!!errors.email}
        helperText={errors.email?.message}
        sx={{ mt: 2 }}
      />

      <TextField
        {...register('password')}
        label="Lösenord"
        type={showPassword ? 'text' : 'password'}
        fullWidth
        autoComplete="new-password"
        error={!!errors.password}
        helperText={errors.password?.message}
        sx={{ mt: 2 }}
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
        sx={{ mt: 2, mb: 3 }}
      />

      <Button
        type="submit"
        variant="contained"
        fullWidth
        size="large"
        disabled={isLoading}
      >
        {isLoading ? <CircularProgress size={24} /> : 'Skapa konto'}
      </Button>
    </Box>
  );
}
