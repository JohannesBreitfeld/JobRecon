const errorTranslations: Record<string, string> = {
  // Auth / Identity
  'User.InvalidCredentials': 'Felaktig e-postadress eller losenord.',
  'User.EmailExists': 'En anvandare med denna e-postadress finns redan.',
  'User.EmailAlreadyExists': 'En anvandare med denna e-postadress finns redan.',
  'User.CreationFailed': 'Kunde inte skapa kontot. Kontrollera att losenordet uppfyller kraven.',
  'User.WeakPassword': 'Losenordet uppfyller inte sakerhetskraven.',
  'User.EmailNotConfirmed': 'E-postadressen har inte bekraftats.',
  'User.AccountDeactivated': 'Kontot har inaktiverats.',
  'User.NotFound': 'Anvandaren hittades inte.',
  'Auth.InvalidCredentials': 'Felaktig e-postadress eller losenord.',
  'Auth.AccountDeactivated': 'Kontot har inaktiverats.',
  'Auth.InvalidToken': 'Ogiltig eller utgangen token.',
  'Auth.ResetFailed': 'Kunde inte aterstalla losenordet. Forsok igen.',
  'Auth.ConfirmationFailed': 'Kunde inte bekrafta e-postadressen.',
  'Auth.UserNotFound': 'Anvandaren hittades inte.',
  'Token.Invalid': 'Ogiltig eller utgangen token.',
  'Token.Expired': 'Token har gatt ut. Logga in igen.',
  'Token.Revoked': 'Token har aterkallats. Logga in igen.',
  'Token.InvalidRefreshToken': 'Sessionen har gatt ut. Logga in igen.',
  'ExternalLogin.ProviderNotSupported': 'Inloggningsmetoden stods inte.',
  'ExternalLogin.AlreadyLinked': 'Kontot ar redan kopplat till denna leverantor.',
  'ExternalLogin.NotLinked': 'Kontot ar inte kopplat till denna leverantor.',

  // Profile
  'Profile.NotFound': 'Profilen hittades inte.',
  'Profile.AlreadyExists': 'En profil finns redan for denna anvandare.',
  'Skill.NotFound': 'Kompetensen hittades inte.',
  'Skill.AlreadyExists': 'Kompetensen finns redan.',
  'JobPreference.NotFound': 'Jobbpreferensen hittades inte.',
  'CVDocument.NotFound': 'CV-dokumentet hittades inte.',

  // Jobs
  'Job.NotFound': 'Jobbannonsen hittades inte.',
  'SavedJob.NotFound': 'Det sparade jobbet hittades inte.',
  'SavedJob.AlreadyExists': 'Jobbannonsen ar redan sparad.',
  'JobSource.NotFound': 'Jobbkallan hittades inte.',
  'JobSource.AlreadyExists': 'Jobbkallan finns redan.',
};

const defaultMessages: Record<number, string> = {
  400: 'Ogiltigt forfragan.',
  401: 'Du ar inte inloggad. Logga in och forsok igen.',
  403: 'Du har inte behorighet att utfora denna atgard.',
  404: 'Resursen hittades inte.',
  409: 'En konflikt uppstod. Forsok igen.',
  500: 'Ett ovantant serverfel uppstod. Forsok igen senare.',
};

const GENERIC_ERROR = 'Ett ovantat fel uppstod.';

export function translateError(
  code: string | undefined,
  fallback?: string,
  status?: number
): string {
  if (code && code in errorTranslations) {
    return errorTranslations[code];
  }
  if (fallback) {
    return fallback;
  }
  if (status && status in defaultMessages) {
    return defaultMessages[status];
  }
  return GENERIC_ERROR;
}
