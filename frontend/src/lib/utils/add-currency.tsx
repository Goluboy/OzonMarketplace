export function addCurrency(amount: number, currency: string = '₽'): 
string {
	return `${amount.toLocaleString('ru-RU')} ${currency}`;
}