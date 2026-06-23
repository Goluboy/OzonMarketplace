
import { PagesConfig } from "@/config/pages.config"
import { Heart, Package, ShoppingBasket, User } from "lucide-react"

export const HeaderMenu = [

    {
        title: "Заказы",
        icon: Package,
        href: PagesConfig.ORDERS,
        disabled: false
    },
    {
        title: "Избранное",
        icon: Heart,
        href: PagesConfig.FAVORITES,
        disabled: true
    },
    {
        title: "Коризна",
        icon: ShoppingBasket,
        href: PagesConfig.CART,
        disabled: false
    }
] as const