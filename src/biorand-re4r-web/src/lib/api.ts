
class Api {
    private urlBase = "http://localhost:10285/api"

    async register(email: string, name: string) {
        return await this.post("auth/register", {
            email,
            name
        });
    }

    async signIn(email: string, code?: string) {
        return await this.post("auth/signin", {
            email,
            code
        });
    }

    private async post(query: string, body?: any) {
        const req = await fetch(this.getUrl(query), {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(body)
        });
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
