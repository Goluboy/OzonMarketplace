import Link from "next/link"

interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {}

export function Button({
  className,
  children,
  ...props
}: ButtonProps) {
  return (
    <Link
    href='/checkout'
      className={`
        inline-flex
        items-center
        justify-center
        rounded-xl
        font-medium
        transition-colors
        ${className ?? ""}
      `}
      {...props}
    >
      {children}
    </Link>
  );
}