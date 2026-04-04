import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Button,
  TextField,
  Alert,
  CircularProgress,
  InputAdornment,
  IconButton,
  Link,
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { useAuthStore } from '../../stores/authStore';

const loginSchema = z.object({
  email: z.string().email('Ogiltig e-postadress'),
  password: z.string().min(1, 'Lösenord krävs'),
});

type LoginFormData = z.infer<typeof loginSchema>;

interface LoginFormProps {
  onSuccess?: () => void;
}

export function LoginForm({ onSuccess }: LoginFormProps) {
  const [showPassword, setShowPassword] = useState(false);
  const { login, isLoading, error, clearError } = useAuthStore();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data);
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

      <TextField
        {...register('email')}
        label="E-post"
        type="email"
        fullWidth
        autoComplete="email"
        autoFocus
        error={!!errors.email}
        helperText={errors.email?.message}
        sx={{ mb: 2 }}
      />

      <TextField
        {...register('password')}
        label="Lösenord"
        type={showPassword ? 'text' : 'password'}
        fullWidth
        autoComplete="current-password"
        error={!!errors.password}
        helperText={errors.password?.message}
        sx={{ mb: 1 }}
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

      <Box sx={{ textAlign: 'right', mb: 2 }}>
        <Link component={RouterLink} to="/forgot-password" variant="body2">
          Glömt lösenord?
        </Link>
      </Box>

      <Button
        type="submit"
        variant="contained"
        color="secondary"
        fullWidth
        size="large"
        disabled={isLoading}
        sx={{ py: 1.3 }}
      >
        {isLoading ? <CircularProgress size={24} /> : 'Logga in'}
      </Button>
    </Box>
  );
}
