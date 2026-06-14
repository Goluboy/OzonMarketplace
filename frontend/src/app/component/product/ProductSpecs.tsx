export function ProductSpecs() {
  return (
    <section className="mt-16">
      <h2 className="text-2xl font-medium mb-6">
        Характеристики
      </h2>

      <div className="space-y-4">
        <div className="flex justify-between border-b pb-3">
          <span>Бренд</span>
          <span>Apple</span>
        </div>

        <div className="flex justify-between border-b pb-3">
          <span>Память</span>
          <span>256 GB</span>
        </div>

        <div className="flex justify-between border-b pb-3">
          <span>Цвет</span>
          <span>Titanium</span>
        </div>
      </div>
    </section>
  );
}