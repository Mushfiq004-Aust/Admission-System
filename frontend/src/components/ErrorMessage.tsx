export function ErrorMessage({ message }: { message: string }) {
  return (
    <p className="alert alert-error" role="alert">
      {message}
    </p>
  );
}
