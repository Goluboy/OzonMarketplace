// app/component/home/itemCard/ItemGrid.tsx
import { ItemCard, type Item } from "./ItemCard";
import { cn } from "@/lib/utils/cn";

interface ItemGridProps {
  items: Item[];
  loading?: boolean;
  className?: string;
}

export function ItemGrid({ items, loading = false, className }: ItemGridProps) {
  if (loading) {
    return (
      <div
        style={{ gridTemplateColumns: "repeat(auto-fill, minmax(364px, 1fr))" }}
        className={cn(
          "grid gap-3 md:gap-4" ,
          className
        )}
      >
        {[...Array(10)].map((_, i) => (
          <div key={i} className="bg-white rounded-xl border border-gray-100 overflow-hidden animate-pulse">
            <div className="aspect-square bg-gray-200"></div>
            <div className="p-3 space-y-2">
              <div className="h-5 bg-gray-200 rounded w-1/2"></div>
              <div className="h-4 bg-gray-200 rounded w-3/4"></div>
              <div className="h-3 bg-gray-200 rounded w-1/3"></div>
            </div>
            <div className="px-3 pb-3">
              <div className="h-9 bg-gray-200 rounded-lg"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-gray-400">
        <span className="text-5xl mb-4">🔍</span>
        <p className="text-lg font-medium">Ничего не найдено</p>
        <p className="text-sm mt-1">Попробуйте изменить запрос или фильтры</p>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5 gap-3 md:gap-4",
        className
      )}
    >
      {items.map((item) => (
        <ItemCard key={item.id} item={item} />
      ))}
    </div>
  );
}