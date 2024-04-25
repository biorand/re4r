
class Api {
    private urlBase = "http://localhost:10285/api"

    async register(email: string, name: string) {
        return await this.post("auth/register");
    }

    private async post(query: string) {
        const req = await fetch(this.getUrl(query));
        const data = await req.json();
        return {
            success: req.status == 200,
            data
        };
    }

    private getUrl(query: string) {
        return `${this.urlBase}/${query}`;
    }
}

export function getApi() {
    return new Api();
}
