import Link from "next/link"
import type { ComponentPropsWithoutRef, ReactNode } from "react"

// Обычная кнопка
interface ButtonProps extends ComponentPropsWithoutRef<"button"> {
  children: ReactNode
  className?: string
}

export function Button({ className, children, ...props }: ButtonProps) {
  return (
    <button
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
    </button>
  )
}

// Кнопка-ссылка
interface ButtonLinkProps extends ComponentPropsWithoutRef<typeof Link> {
  children: ReactNode
  className?: string
}

export function ButtonLink({ className, children, ...props }: ButtonLinkProps) {
  return (
    <Link
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
  )
}