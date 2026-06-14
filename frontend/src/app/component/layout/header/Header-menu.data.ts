
import { PagesConfig } from "@/config/pages.config"
import { Heart, Package, ShoppingBasket, User } from "lucide-react"

export const HeaderMenu = [

    {
        title: "Заказы",
        icon: Package,
        href: PagesConfig.ORDERS
    },
    {
        title: "Избранное",
        icon: Heart,
        href: PagesConfig.FAVORITES
    },
    {
        title: "Коризна",
        icon: ShoppingBasket,
        href: PagesConfig.CART
    }
] as const